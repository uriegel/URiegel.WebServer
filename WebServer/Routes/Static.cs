using System;
using System.Threading.Tasks;

namespace UwebServer.Routes
{
    public class Static : Route
    {
        public string FilePath { get; set; }
        public override async Task<bool> ProcessAsync(IRequest request, IRequestHeaders requestHeaders, Response response)
        {
            try 
            {
                var file = requestHeaders.Url == Path || requestHeaders.Url == "/" && Path == null
                    ? System.IO.Path.Combine(FilePath, "index.html")
                    : System.IO.Path.Combine(FilePath, requestHeaders.Url[((Path?.Length ?? 1) == 1 ? 1 : Path.Length + 1)..]);
                await response.SendFileAsync(file);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error static route, url: {requestHeaders.Url}, path: {Path}, {e}");
                await response.SendNotFoundAsync();
            }
            return true;
        }
    }
}
