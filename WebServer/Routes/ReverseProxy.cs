using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UwebServer.Routes
{
    public class ReverseProxy : Route
    {
        public ReverseProxy(string path, string redirectUrl)
        {
            Path = path;
            this.redirectUrl = redirectUrl;
        }

        public override async Task<bool> ProcessAsync(IRequest request, IRequestHeaders headers, Response response)
        {
            var query = new UrlComponents(headers.Url, Path);
            var urlExtension = headers.Url.Length > Path.Length ? headers.Url[Path.Length..] : "";
            var internalRequest = (IInternalRequest)request;

            try
            {
                // TODO: WS
				// if (CheckWsUpgrade())
				// {
				// 	var webSocketProxy = new WebSocketProxy(url);
                //     try
                //     {
                //         await webSocketProxy.Initialize();
                //     }
                //     catch (Exception e)
                //     {
                //         Logger.Current.Trace($"Error Redirect Websocket: {e}");
                //         throw new Exception($"Proxy Redirection: {Headers.Method} not supported");
                //     }
                //     var webSocketSession = await UpgradeWebSocketAsync(false);
                //     var webSocket = new WebSocket(webSocketSession);
                //     webSocketProxy.ConnectCallbacks(webSocket);
                //     return true;
				// }

				if (headers.Method != Method.GET && headers.Method != Method.POST)
                    throw new Exception($"Redirection: {headers.Method} not supported");

                var webRequest = (HttpWebRequest)WebRequest.Create(redirectUrl + urlExtension);
                webRequest.Method = headers.Method.ToString();

                var body = headers.Method == Method.POST ? await GetAsync(request) : null;

				// User-Agent, Referer??
                foreach (var h in headers.Raw)
                {
                    switch (h.Key.ToLower())
                    {
                        case "accept":
                            webRequest.Accept = h.Value.Value;
                            break;
                        case "connection":
                            if (h.Value.Value != "Keep-Alive")
                                webRequest.KeepAlive = false;
                            break;
                        case "if-modified-since":
                            {
                                var dts = h.Value.Value;
                                var pos = dts.IndexOf(';');
                                if (pos != -1)
                                    dts = dts.Substring(0, pos);
                                var dt = DateTime.Parse(dts.Trim());
                                webRequest.IfModifiedSince = dt;
                            }
                            break;
                        case "content-length":
                            {
                                if (int.TryParse(h.Value.Value, out var cl))
                                    webRequest.ContentLength = cl;
                                else
                                    Console.WriteLine($"{request.Id} Could not set Content-Length");
                            }
                            break;
                        case "content-type":
                            webRequest.ContentType = h.Value.Value;
                            break;
                        case "host":
                            break;
                        case "user-agent":
                            webRequest.UserAgent = h.Value.Value;
                            break;
                        case "range":
                            try
                            {
                                var sizes = h.Value.Value.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1)
                                    .Select(n => long.Parse(n)).ToArray();
                                if (sizes.Length > 1)
                                    webRequest.AddRange(sizes[0], sizes[1]);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"{request.Id} Error occurred in range: {e}");
                            }
                            break;
                        case "referer":
                            webRequest.Referer = h.Value.Value;
                            break;
                        default:
                            {
                                try
                                {
                                    webRequest.Headers.Add(h.Value.Key + ": " + h.Value.Value);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"{request.Id} Could not redirect: {e}");
                                }
                            }
                            break;
                    }
                }
                // if (addXForwardedUri)
                //     webRequest.Headers.Add($"X-Forwarded-URI: {CreateXForwarded()}");

                if (body != null)
                    using (var requestStream = await webRequest.GetRequestStreamAsync())
                        await requestStream.WriteAsync(body, 0, body.Length);

                HttpWebResponse webResponse = null;
                try
                {
                    webRequest.CertificateValidator(e =>
                    {
                        Console.WriteLine($"{request.Id} {e.Message}");
                        e.ChainErrorDescriptions?.Perform(n =>
                        {
                            Console.WriteLine($"{request.Id} {n}");
                            return true;
                        });
                        return false;
                    });
                    webResponse = (HttpWebResponse)await webRequest.GetResponseAsync();
                }
                catch (WebException we) when (we.Response != null)
                {
                    webResponse = (HttpWebResponse)we.Response;
                }
                var strom = webResponse.GetResponseStream();

                var responseHeaders = webResponse.Headers.AllKeys.Select(n => string.Format("{0}: {1}", n, webResponse.Headers[n]));
                //if (Tracing.Current.IsEnabled && Tracing.Current.IsHttpHeaderEnabled)
                //    Tracing.Current.TraceGetResponseHeaders(responseHeaders);
                responseHeaders = responseHeaders.Where(n => !n.StartsWith("allow:", StringComparison.InvariantCultureIgnoreCase)
                    && !n.StartsWith("connection:", StringComparison.InvariantCultureIgnoreCase));
                var headerString = string.Join("\r\n", responseHeaders) + "\r\n\r\n";
                var html = $"{request.HttpResponseString} {(int)webResponse.StatusCode} {webResponse.StatusDescription}\r\n" + headerString;
                var htmlBytes = Encoding.UTF8.GetBytes(html);
                await internalRequest.WriteAsync(htmlBytes, 0, htmlBytes.Length);
                await WriteStreamAsync(internalRequest, strom);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error has occurred while redirecting: {e}");
                try
                {
                    await response.SendExceptionAsync(e);
                }
                catch { }
				return false;
            }
            return true;
        }

  		async Task<byte[]> GetAsync(IRequest request)
        {
			using var ms = new MemoryStream();
			await request.ReadStreamAsync(ms);
			ms.Position = 0;
			var result = new byte[ms.Length];
			ms.Read(result, 0, result.Length);
			return result;
		}

        async Task WriteStreamAsync(IInternalRequest request, Stream stream)
        {
            var bytes = new byte[8192];
            while (true)
            {
                var read = await stream.ReadAsync(bytes, 0, bytes.Length);
                if (read == 0)
                    return;
                await request.WriteAsync(bytes, 0, read);
            }
        }

        readonly string redirectUrl;
    }
}
