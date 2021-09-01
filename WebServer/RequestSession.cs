using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UwebServer
{
    class RequestSession : IRequest
    {
        public DateTime? RequestStartTime { get; private set; }
        public DateTime RequestStart { get => RequestStartTime ?? DateTime.Now; }
        public IRequestHeaders Headers { get; private set; } = new RequestHeaders();
        public bool IsSecureConnection { get { return SocketSession?.UseTls ?? false; } }
		public IPEndPoint LocalEndPoint { get; private set; }
		public IPEndPoint RemoteEndPoint { get; private set; }
        public long BytesSent { get; private set; }

        public string UrlRoot
        {
            get
            {
                if (_UrlRoot == null)
                    _UrlRoot = $"http{(Server.Settings.IsTlsEnabled ? "s" : null)}://{Headers.Host}";
                return _UrlRoot;
            }
        }
        string _UrlRoot;

        public string Id { get; }

        public SocketSession SocketSession { get; private set; }

        public Server Server { get; private set; }
    
        public string HttpResponseString
        {
            get
            {
                if (_HttpResponseString == null && Headers is RequestHeaders requestHeaders)
                    _HttpResponseString = requestHeaders.Http10 ? "HTTP/1.0" : "HTTP/1.1";
                return _HttpResponseString ?? "";
            }
        }
        string _HttpResponseString;

        public RequestSession(Server server, SocketSession socketSession, Stream networkStream, DateTime? startTime)
        {
            RequestStartTime = startTime;
            Id = socketSession.Id + "-" + Interlocked.Increment(ref sessionIDCreator);
            responseHeaders = new ServerResponseHeaders(server, Id);
            stopwatch = new Stopwatch();

            SocketSession = socketSession;
            Server = server;
            this.networkStream = networkStream;

			LocalEndPoint = (SocketSession.Client.Client.LocalEndPoint as IPEndPoint)!;
			RemoteEndPoint = (SocketSession.Client.Client.RemoteEndPoint as IPEndPoint)!;

            Console.WriteLine($"{Id} New request session created - {RemoteEndPoint}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True: Continue in this SocketSession, otherwise stops processing</returns>
        public async Task<bool> StartAsync()
        {
            try
            {
                var cts = new CancellationTokenSource(Server.Settings.SocketLifetime);
                var read = await networkStream.ReadAsync(readBuffer, 0, readBuffer.Length, cts.Token);
                if (read == 0)
                {
                    Console.WriteLine($"{Id} Socket session closed");
                    return false;
                }
                if (!RequestStartTime.HasValue)
                    RequestStartTime = DateTime.Now;
                return await ReceiveAsync(read);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"{Id} Closing socket session, lifetime exceeded");
                Close(true);
                return false;
            }
            catch (Exception e) when (e is IOException || e is ConnectionClosedException || e is SocketException)
            {
                Console.WriteLine($"{Id} Closing socket session: {e}");
                Close(true);
                return false;
            }
            catch (Exception e) when (e is ObjectDisposedException)
            {
                Console.WriteLine($"{Id} Object disposed");
                Close(true);
                return false;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{Id} An error has occurred while reading socket: {e}");
                Close(true);
                return false;
            }
        }

        public void Close(bool fullClose)
        {
            try
            {
                if (fullClose)
                {
                    networkStream.Close();
                    isClosed = true;
                }
                else
                    SocketSession.Client.Client.Shutdown(SocketShutdown.Send);
            }
            catch { }
        }

        public async Task WriteAsync(byte[] buffer, int offset, int length)
        {
            await networkStream.WriteAsync(buffer, offset, length);
            BytesSent += length;
        }

        public async Task ReadStreamAsync(Stream stream)
        {
            var cls = Headers["content-length"];
            var length = long.Parse(cls ?? "0");

            while (length > 0)
            {
                int read;
                if (readFromBuffer)
                {
                    var cache = bufferReadCount - bufferEndPosition;
                    if (cache > 0)
                        read = (int)Math.Min(length, (long)cache);
                    else
                    {
                        readFromBuffer = false;
                        continue;
                    }
                }
                else
                {
                    var readLength = (int)Math.Min((long)readBuffer.Length, length);
                    read = await networkStream.ReadAsync(readBuffer, 0, readLength);
                    if (read == 0 && readLength > 0)
                        throw new ConnectionClosedException();
                }
                length -= read;
                await stream.WriteAsync(readBuffer, readFromBuffer ? bufferEndPosition : 0, read);
                readFromBuffer = false;
            }
        }

        async Task<bool> ReceiveAsync(int bufferPosition)
        {
            try
            {
                var result = await (Headers as Headers)!.InitializeAsync(networkStream, readBuffer, bufferPosition, false /*Server.Settings.HeaderTracing */, Id);
                bufferEndPosition = result.BufferEndPosition;
                readFromBuffer = bufferEndPosition > 0;
                bufferReadCount = result.BufferReadCount;

                Console.WriteLine($"{Id} Request: {RemoteEndPoint} \"{Headers.Method} {Headers.Url} {Headers.Http}\"");

                stopwatch?.Start();

				//string refererPath = null;
				//if (headers.ContainsKey("Referer"))
				//{
				//    string path = headers["Referer"];
				//    path = path.Substring(path.IndexOf("//") + 2);
				//    refererPath = path.Substring(path.IndexOf('/') + 1);
				//}

				// if (Headers.Method == Method.OPTIONS)
                //     return ServeOptions();

                return await ProcessRoutes();

                async Task<bool> ProcessRoutes()
                {
                    var response = new Response(this, responseHeaders);
                    foreach (var route in Server.Settings.Routes)
                    {
                        if (route.Method != Method.UNDEFINED && route.Method != Headers.Method)
                            continue;
                        if (route.Path != null && !Headers.Url.StartsWith(route.Path, true, null))
                            continue;
                        if (route.BasicAuthentication != null)
                        {
                            if (! await route.BasicAuthentication.Authenticate(response, Headers))
                                return false;
                        }
                        if (!await route.ProcessAsync(this, Headers, response))
                            continue;
                        return true;
                    }
                    await response.SendNotFoundAsync();
                    return false;
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.TimedOut)
                {
                    Console.Error.WriteLine($"{Id} Socket session closed, Timeout has occurred");
                    Close(true);
                    return false;
                }
                return true;
            }
            catch (ConnectionClosedException)
            {
                Console.WriteLine($"{Id} Socket session closed via exception");
                Close(true);
                return false;
            }
			catch (ObjectDisposedException oe)
            {
                Console.WriteLine($"{Id} Socket session closed, an error has occurred: {oe}");
                Close(true);
                return false;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{Id} Socket session closed, an error has occurred while receiving: {e}");
                Close(true);
                return false;
            }
            finally
            {
                var elapsed = stopwatch?.Elapsed;
                stopwatch?.Stop();
                Console.WriteLine($"{Id} Answer: {RemoteEndPoint} \"{Headers.Method} {Headers.Url} {Headers.Http}\" Status: {responseHeaders.Status} Size: {responseHeaders.ContentLength} Duration: {elapsed}");
            }
        }

        static int sessionIDCreator;
        readonly object locker = new object();

        Stream networkStream;
        // has to be short living!!
        byte[] readBuffer = new byte[80000]; // Has to be under 85kB, otherwise the buffer gets to LOH
        
        ServerResponseHeaders responseHeaders;

        /// <summary>
        /// Position in buffer at which header data ends and payload starts, immediately after reading header from network stream
        /// </summary>
        int bufferEndPosition;
        /// <summary>
        /// If payload is read and available in buffer
        /// </summary>
        bool readFromBuffer;
        /// <summary>
        /// Number of read bytes already in buffer, immediately after reading header from network stream
        /// </summary>
        int bufferReadCount;
        bool isClosed;
        readonly Stopwatch stopwatch;
    }
}