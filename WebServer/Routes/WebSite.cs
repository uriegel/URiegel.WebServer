using System;
using System.IO;
using System.Threading.Tasks;

namespace UwebServer.Routes
{
    public class WebSite : Route
    {
        public WebSite(Func<string, Stream> getStream) : this(getStream, _ => null) {}
        public WebSite(Func<string, Stream> getStream, Func<string, DateTime?> getLastModified) 
        {
            this.getStream = getStream;
            this.getLastModified = getLastModified;
        } 

        public override async Task ProcessAsync(IRequest request, IRequestHeaders requestHeaders, Response response)
        {
            try 
            {
                var file = requestHeaders.Url == Path || requestHeaders.Url == "/" && Path == null
                    ? "index.html"
                    : System.IO.Path.Combine(requestHeaders.Url[((Path?.Length ?? 1) == 1 ? 1 : Path.Length + 1)..]);
                
                await response.SendFileStreamAsync(file, getStream, getLastModified(file));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error static route, url: {requestHeaders.Url}, path: {Path}, {e}");
                await response.SendNotFoundAsync();
            }
        }

        readonly Func<string, Stream> getStream;
        readonly Func<string, DateTime?> getLastModified;
    }
}
