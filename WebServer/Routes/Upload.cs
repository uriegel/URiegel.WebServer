using System.IO;
using System.Threading.Tasks;
using UwebServer.Routes;

namespace UwebServer
{
    public class UploadRoute : Route
    {
        public UploadRoute(string path, string uploadPath)
        {
            Method = Method.POST;
            Path = path;
            this.uploadPath = uploadPath;
        }

        public override async Task<bool> ProcessAsync(IRequest request, IRequestHeaders requestHeaders, Response response)
        {
            long.TryParse(requestHeaders["Content-Length"], out var length);
            var query = new UrlComponents(requestHeaders.Url);
            var filename = System.IO.Path.Combine(uploadPath, query.Parameters["file"]);
            using var file = File.Create(filename);
            await request.ReadStreamAsync(file);
            return true;
        }

        string uploadPath;
    }
}