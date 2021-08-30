using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace UwebServer
{
    class SocketSession 
    {
        public DateTime ConnectTime { get; } = DateTime.Now;

        public int Id { get; private set; }

        public TcpClient Client { get; }

        public bool UseTls { get; }


        public SocketSession(Server server, TcpClient client, bool useTls)
        {
            UseTls = useTls;
            Id = Interlocked.Increment(ref lastId);
            Console.WriteLine($"{Id}- New {(useTls ? "secure " : "")}socket session created: - {(client.Client.RemoteEndPoint as IPEndPoint)}");
            this.server = server;
            Client = client;
            client.ReceiveTimeout = (int)server.Settings.SocketTimeout.TotalMilliseconds;
            client.SendTimeout = (int)server.Settings.SocketTimeout.TotalMilliseconds;
        }

        public static async void StartReceiving(Server server, TcpClient tcpClient, bool isSecured)
        {
			var session = new SocketSession(server, tcpClient, isSecured);
			await session.ReceiveAsync();
            tcpClient.Close();
        }

		public async Task ReceiveAsync()
        {
            try
            {
                while (true)
                {
                    DateTime? startTime = null;
                    if (networkStream == null)
                    {
                        networkStream = UseTls ? await GetTlsNetworkStreamAsync(Client) : Client.GetStream();
                        startTime = ConnectTime;
                    }

					var session = new RequestSession(server, this, networkStream, startTime);
					if (!await session.StartAsync())
						break;
				}
            }
            catch (AuthenticationException ae)
            {
                Console.WriteLine($"{Id}- An authentication error has occurred while reading socket, session: {Client.Client.RemoteEndPoint as IPEndPoint}, error: {ae}");
            }
            catch (Exception e) when (e is IOException || e is ConnectionClosedException || e is SocketException)
            {
                Console.WriteLine($"{Id}- Closing socket session, reason: {e}");
                Close();
            }
            catch (Exception e) when (e is ObjectDisposedException)
            {
                Console.WriteLine($"{Id}- Object disposed");
                Close();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{Id}- An error has occurred while reading socket, error: {e}");
            }
        }

        public void Close() => Client.Close();

        async Task<Stream> GetTlsNetworkStreamAsync(TcpClient tcpClient)
        {
            var stream = tcpClient.GetStream();
            if (!server.Settings.IsTlsEnabled)
                return null;

            var sslStream = new SslStream(stream);
            await sslStream.AuthenticateAsServerAsync(server.Settings.Certificate, false, server.Settings.TlsProtocols, server.Settings.CheckRevocation);

            Console.WriteLine($"{Id}- Secure protocol: {sslStream.SslProtocol}, Cipher: {sslStream.CipherAlgorithm} strength {sslStream.CipherStrength}, Key exchange: {GetKeyExchangeAlgorithm(sslStream)} strength {sslStream.KeyExchangeStrength}, Hash: {GetHashAlgorithm(sslStream)} strength {sslStream.HashStrength}");
            string GetKeyExchangeAlgorithm(SslStream n) => (int)n.KeyExchangeAlgorithm == 44550 ? "ECDHE" : $"{n.KeyExchangeAlgorithm}";
            string GetHashAlgorithm(SslStream n)
            {
                switch ((int)n.HashAlgorithm)
                {
                    case 32781:
                        return "SHA384";
                    case 32780:
                        return "SHA256";
                    default:
                        return $"{n.HashAlgorithm}";
                }
            }

            return sslStream;
        }

        readonly Server server;
        static int lastId;
        protected Stream networkStream;
    }
}
