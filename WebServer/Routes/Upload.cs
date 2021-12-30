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
            var query = new UrlComponents(requestHeaders.Url, Path);
            if (!string.IsNullOrEmpty(query.Path))
            {
                var folderPath = System.IO.Path.Combine(uploadPath, query.Path);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
            }
            var filename = System.IO.Path.Combine(uploadPath, query.Path, query.Parameters["file"]);
            using var file = File.Create(filename);
            await request.ReadStreamAsync(file);
            response.SendOkAsync();
            return true;
        }

        string uploadPath;
    }
}