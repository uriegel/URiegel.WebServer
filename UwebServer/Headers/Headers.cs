using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UwebServer
{
	/// <summary>
	/// All HTTP header, Access is case insensitiv, as HTTP header ares case insensitv (RFC 7230, RFC 2616)
	/// You can access all HTTP headers with property <see cref="Raw"/>
	/// </summary>
    abstract class Headers
    {
        public struct Result
        {
            public Result(int bufferEndPosition, int bufferReadCount)
            {
                BufferEndPosition = bufferEndPosition;
                BufferReadCount = bufferReadCount;
            }

            /// <summary>
            /// Position in buffer at which header data ends and payload starts, immediately after reading header from network stream
            /// </summary>
            public int BufferEndPosition { get; private set; }
            /// <summary>
            /// Number of read bytes already in buffer, immediately after reading header from network stream
            /// </summary>
            public int BufferReadCount { get; private set; }
        }

        public string ContentType
        {
            get
            {
                if (_ContentType == null)
                {
                    if (headers.TryGetValue("content-type", out var kvp))
                        _ContentType = kvp.Value;
                    else
                        _ContentType = "";
                }
                return _ContentType;
            }
        }
        string _ContentType;

        /// <summary>
        /// Access to header value. 
        /// </summary>
        /// <param name="key">Key for access to header value. The key is case insensitiv</param>
        /// <returns>Value of header, or null, if it is not existent</returns>
        public string this[string key]
        {
            get
            {
                if (!headers.TryGetValue(key, out var kvp))
                    return null;
                return kvp.Value ?? ""; 
            }
        }

        public IEnumerable<KeyValuePair<string, KeyValuePair<string, string>>> Raw
        {
            get => headers.AsEnumerable<KeyValuePair<string, KeyValuePair<string, string>>>();
        }

        /// <summary>
        /// Reading headerfrom network stream. 
        /// </summary>
        /// <param name="networkStream">Network stream containg headers</param>
        /// <param name="buffer">Already read data from network stream</param>
        /// <param name="recentbufferPosition">Actual position in network stream</param>
        /// <param name="tracing"></param>
        /// <param name="sessionID"></param>
        public async Task<Result> InitializeAsync(Stream networkStream, byte[] buffer, int recentbufferPosition, bool tracing, object sessionID)
        {
            var result = await ReadHeaderFromStreamAsync(networkStream, buffer, recentbufferPosition);
            var headerParts = result.headerString.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            InternalInitialize(headerParts);

            try
            {
                var keyValues = headerParts.Skip(1).Select(s => new KeyValuePair<string, string>(s.Substring(0, s.IndexOf(": ")), s.Substring(s.IndexOf(": ") + 2)));
                foreach (var keyValue in keyValues)
                    headers[keyValue.Key] = keyValue;
            }
            catch (Exception)
            {
                headers.Clear();
                var keyValues = headerParts.Skip(1).Select(s =>
                {
                    try
                    {
                        return new KeyValuePair<string, string>(s.Substring(0, s.IndexOf(": ")), s.Substring(s.IndexOf(": ") + 2));
                    }
                    catch { return new KeyValuePair<string, string>("_OBSOLETE_", ""); }
                }).Where(n => n.Key != "_OBSOLETE_");
                foreach (var keyValue in keyValues)
                    headers[keyValue.Key] = keyValue;
            }

            InternalTracing(sessionID, headerParts[0]);
            if (tracing)
                foreach (var header in headers)
                    Console.WriteLine($"{sessionID} {header.Value.Key}: {header.Value.Value}");
            return result.result;
        }

        protected abstract void InternalInitialize(string[] headerParts);

        protected virtual void InternalTracing(object sessionId, string request) {}

        async Task<(string headerString, Result result)> ReadHeaderFromStreamAsync(Stream networkStream, byte[] buffer, int recentbufferPosition)
        {
            var index = 0;
            var read = recentbufferPosition;
            while (true)
            {
                for (var i = index; i < Math.Min(read + index, buffer.Length); i++)
                {
                    if (i > 4 && buffer[i] == '\n' && buffer[i - 1] == '\r' && buffer[i - 2] == '\n')
                        return (Encoding.ASCII.GetString(buffer, 0, i - 1), new Result(i + 1, index + read));
                }
                index += read;
                read = await networkStream.ReadAsync(buffer, index, buffer.Length - index);
                if (read == 0)
                    throw new ConnectionClosedException();
            }
        }

        protected Dictionary<string, KeyValuePair<string, string>> headers = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.OrdinalIgnoreCase);
    }
}
