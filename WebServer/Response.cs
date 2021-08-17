using System;
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

        public async Task SendNotFoundAsync()
        {
            var status = 404;
            var statusText = "Not found";
            var htmlHead = CreateErrorHead(status, statusText);
            var htmlBody = $"<h2>Not found</h2><h3>The requested resource could not be found</h3>";
            await SendErrorAsync(htmlHead, htmlBody, status, statusText);
            Console.WriteLine($"{requestSession.Id} {status} {statusText}");
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
        readonly RequestSession requestSession;
        readonly ServerResponseHeaders responseHeaders;
    }
}