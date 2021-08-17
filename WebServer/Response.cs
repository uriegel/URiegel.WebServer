using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace UwebServer
{
    public class Response
    {
        internal Response(RequestSession requestSession, ServerResponseHeaders responseHeaders)
        {
            this.requestSession = requestSession;
            this.responseHeaders = responseHeaders;
        }

        public async Task SendFileAsync(string file)
        {
            {
                if (file.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase)
                    || file.EndsWith(".mkv", StringComparison.InvariantCultureIgnoreCase)
                    || file.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase)
                    || file.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase))
                    await SendRangeAsync(file);
                else
                    await InternalSendFileAsync(file);
            }
        }

        public async Task SendNotFoundAsync()
        {
            var status = 404;
            var statusText = "Not found";
            var htmlHead = CreateErrorHead(status, statusText);
            var htmlBody = $"<h2>Not found</h2><h3>The requested resource could not be found</h3>";
            await SendErrorAsync(htmlHead, htmlBody, status, statusText);
            Console.WriteLine($"{requestSession.Id} {status} {statusText}");
        }

        public async Task SendNotModifiedAsync()
        {
            var headerString = $"{requestSession.HttpResponseString} 304 Not Modified\r\n\r\n";
            Console.WriteLine($"{requestSession.Id} {headerString}");

            var buffer = ASCIIEncoding.ASCII.GetBytes(headerString);
            responseHeaders.SetInfo(304, 0);
            await requestSession.WriteAsync(buffer, 0, buffer.Length);
        }


        public async Task SendErrorAsync(string htmlHead, string htmlBody, int errorCode, string errorText)
        {
            var response = $"<html><head>{htmlHead}</head><body>{htmlBody}</body></html>";
            var responseBytes = Encoding.UTF8.GetBytes(response);

            responseHeaders.Status = errorCode;
            responseHeaders.StatusDescription = errorText ?? "";
            responseHeaders.Add("Content-Length", $"{responseBytes.Length}");
            responseHeaders.Add("Content-Type", "text/html; charset=UTF-8");
            var headerBuffer = responseHeaders.Access(requestSession.SocketSession.UseTls, requestSession.HttpResponseString, requestSession.Headers);
            await requestSession.WriteAsync(headerBuffer, 0, headerBuffer.Length);
            await requestSession.WriteAsync(responseBytes, 0, responseBytes.Length);
        }

        string CreateErrorHead(int statusCode, string statusText)
            => $@"<meta http-equiv=""Content-Type"" content=""text/html""/>
<title>{statusCode} - {statusText}</title>
<style>
	body {{ font-family:message-box, Sans-Serif; font-size:10pt; }}
	h2 {{ font-weight:bold; font-size:14pt; color:#c03030; }}
	h3 {{ font-weight:bold; font-size:10pt;  }}
	a {{ color:#6666cc; margin-top:0.5em; text-decoration:none; }}
	a:hover {{ text-decoration:underline; }}
	a:focus {{ outline:none; }}
</style>
";

        async Task InternalSendFileAsync(string file)
        {
            var fi = new FileInfo(file);
            if (!fi.Exists)
            {
                Console.WriteLine($"File not found: {file}");
                await SendNotFoundAsync();
                return;
            }
            var noCache = false; // TODO requestSession.Server.Settings.NoCacheFiles.Contains(file.ToLower());

            if (!noCache && Check304(fi.LastWriteTime))
            {
                await SendNotModifiedAsync();
                return;
            }

            var contentType = GetContentType(fi.Extension);
            var dateTime = fi.LastWriteTime;
            var lastModified = dateTime.ToUniversalTime().ToString("r");
            var isImage = contentType?.StartsWith("image/") ?? false;
            try
            {
                using Stream stream = File.OpenRead(file);
                await SendStreamAsync(stream, contentType, lastModified, noCache);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{requestSession.Id} Could not send file: {e}");
            }
        }

        public async Task SendJsonBytesAsync(byte[] json)
        {
            byte[] sendBytes = GetSendBytes(json);
            var contentLength = (int)sendBytes.Length;
            responseHeaders.InitializeJson(contentLength);
            switch (requestSession.Headers.ContentEncoding)
            {
                case ContentEncoding.Deflate:
                    responseHeaders.Add("Content-Encoding", "deflate");
                    break;
                case ContentEncoding.GZip:
                    responseHeaders.Add("Content-Encoding", "gzip");
                    break;
                default:
                    break;
            }
            var tcpPayload = responseHeaders.Access(requestSession.SocketSession.UseTls, requestSession.HttpResponseString, requestSession.Headers, sendBytes);
            await requestSession.WriteAsync(tcpPayload, 0, tcpPayload.Length);

            byte[] GetSendBytes(byte[] json)
            {
                var memStm = new MemoryStream();
                Stream streamToDeserialize;
                switch (requestSession.Headers.ContentEncoding)
                {
                    case ContentEncoding.Deflate:
                        streamToDeserialize = new DeflateStream(memStm, System.IO.Compression.CompressionMode.Compress, true);
                        break;
                    case ContentEncoding.GZip:
                        streamToDeserialize = new GZipStream(memStm, System.IO.Compression.CompressionMode.Compress, true);
                        break;
                    default:
                        return json;
                }
                streamToDeserialize.Write(json, 0, json.Length);
                streamToDeserialize.Close();
                memStm.Capacity = (int)memStm.Length;
                return memStm.GetBuffer();
            }
        }

        public async Task SendStreamAsync(Stream stream, string contentType, string lastModified, bool noCache)
        {
            if (!noCache)
            {
                var isModifiedSince = requestSession.Headers["if-modified-since"];
                if (isModifiedSince == NOT_MODIFIED)
                {
                    await SendNotModifiedAsync();
                    return;
                }
            }

            if (requestSession.Headers.ContentEncoding != ContentEncoding.None &&
                contentType != null &&
                (contentType.StartsWith("application/javascript", StringComparison.InvariantCultureIgnoreCase)
                    || contentType.StartsWith("text/", StringComparison.InvariantCultureIgnoreCase)))
            {
                var ms = new MemoryStream();

                Stream compressedStream;
                switch (requestSession.Headers.ContentEncoding)
                {
                    case ContentEncoding.Deflate:
                        responseHeaders.Add("Content-Encoding", "deflate");
                        compressedStream = new DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, true);
                        break;
                    case ContentEncoding.GZip:
                        responseHeaders.Add("Content-Encoding", "gzip");
                        compressedStream = new GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true);
                        break;
                    default:
                        compressedStream = null;
                        break;
                }
                using (compressedStream)
                {
                    stream.CopyTo(compressedStream!);
                    compressedStream.Close();
                    stream = ms;
                }
                ms.Position = 0;
            }

            responseHeaders.Initialize(contentType, (int)stream.Length, lastModified, noCache);

            if (contentType != null &&
                (contentType.StartsWith("application/javascript", StringComparison.InvariantCultureIgnoreCase)
                || contentType.StartsWith("text/css", StringComparison.InvariantCultureIgnoreCase)
                || contentType.StartsWith("text/html", StringComparison.InvariantCultureIgnoreCase)))
            {
                responseHeaders.Add("Expires", DateTime.Now.ToUniversalTime().ToString("r"));
                //responseHeaders.Add("Cache-Control", "must-revalidate");
                //responseHeaders.Add("Expires", "-1");
            }

            var headerBuffer = responseHeaders.Access(requestSession.SocketSession.UseTls, requestSession.HttpResponseString, requestSession.Headers);
            await requestSession.WriteAsync(headerBuffer, 0, headerBuffer.Length);

            if (requestSession.Headers.Method == Method.HEAD)
                return;

            var bytes = new byte[8192];
            while (true)
            {
                var read = await stream.ReadAsync(bytes, 0, bytes.Length);
                if (read == 0)
                    return;
                await requestSession.WriteAsync(bytes, 0, read);
            }
        }

        async Task SendRangeAsync(string file)
        {
            var fi = new FileInfo(file);
            using Stream stream = File.OpenRead(file);
            await SendRangeAsync(stream, fi.Length, file, null);
        }

		async Task SendRangeAsync(Stream stream, long fileLength, string? file, string? contentType)
		{
			var rangeString = requestSession.Headers["range"];
			if (rangeString == null)
			{
				if (!string.IsNullOrEmpty(file))
					await InternalSendFileAsync(file);
				else
					await SendStreamAsync(stream, contentType, DateTime.Now.ToUniversalTime().ToString("r"), true);
				return;
			}

			rangeString = rangeString.Substring(rangeString.IndexOf("bytes=") + 6);
			var minus = rangeString.IndexOf('-');
			long start = 0;
			var end = fileLength - 1;
			if (minus == 0)
				end = long.Parse(rangeString.Substring(1));
			else if (minus == rangeString.Length - 1)
				start = long.Parse(rangeString.Substring(0, minus));
			else
			{
				start = long.Parse(rangeString.Substring(0, minus));
				end = long.Parse(rangeString.Substring(minus + 1));
			}

			var contentLength = end - start + 1;
			if (string.IsNullOrEmpty(contentType))
				contentType = "video/mp4";
			var headerString =
$@"{requestSession.HttpResponseString} 206 Partial Content
ETag: ""0815""
Accept-Ranges: bytes
Content-Length: {contentLength}
Content-Range: bytes {start}-{end}/{fileLength}
Keep-Alive: timeout=5, max=99
Connection: Keep-Alive
Content-Type: {contentType}

";
			Console.WriteLine($"{requestSession.Id} {headerString}");
			var vorspannBuffer = ASCIIEncoding.ASCII.GetBytes(headerString);
			await requestSession.WriteAsync(vorspannBuffer, 0, vorspannBuffer.Length);
			var bytes = new byte[40000];
			var length = end - start;
			stream.Seek(start, SeekOrigin.Begin);
			long completeRead = 0;
			while (true)
			{
				var read = await stream.ReadAsync(bytes, 0, Math.Min(bytes.Length, (int)(contentLength - completeRead)));
				if (read == 0)
					return;
				completeRead += read;
				await requestSession.WriteAsync(bytes, 0, read);
				if (completeRead == contentLength)
					return;
			}
		}

        bool Check304(DateTime lastWriteTime)
        {
            var isModifiedSince = requestSession.Headers["if-modified-since"];
            if (isModifiedSince != null)
            {
                var pos = isModifiedSince.IndexOf(';');
                if (pos != -1)
                    isModifiedSince = isModifiedSince.Substring(0, pos);
                var ifModifiedSince = Convert.ToDateTime(isModifiedSince);
                var fileTime = lastWriteTime.AddTicks(-(lastWriteTime.Ticks % TimeSpan.FromSeconds(1).Ticks));
                var diff = fileTime - ifModifiedSince;
                return diff <= TimeSpan.FromMilliseconds(0);
            }
            else
                return false;
        }

        string GetContentType(string extension)
            => extension switch
            {
                ".html" => "text/html; charset=UTF-8",
                ".htm" => "text/html; charset=UTF-8",
                ".css" => "text/css; charset=UTF-8",
                ".js" => "application/javascript; charset=UTF-8",
                ".appcache" => "text/cache-manifest",
                _ => mimeTypes[extension]
            };

        readonly RequestSession requestSession;
        readonly ServerResponseHeaders responseHeaders;
        readonly static MimeTypes mimeTypes;
      	const string NOT_MODIFIED = "Fri, 01 Jun 2012 08:28:30 GMT";

    }
}