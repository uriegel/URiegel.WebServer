using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UwebServer.Routes
{
    
    public class JsonService : Route
    {
        public record Input(string Path, IRequestParam RequestParam);

        public Func<Input, Task<object>> OnRequest { get; set;}
        public JsonService(string path, Func<Input, Task<object>> onRequest)
        {
            Method = Method.POST;
            Path = path;
            OnRequest = onRequest;
        } 

        public override async Task ProcessAsync(IRequest request, IRequestHeaders requestHeaders, Response response)
        {
            var path = requestHeaders.Url[(Path.Length+1)..];
            var input = new MemoryStream();
            await request.ReadStreamAsync(input);
            var result = await OnRequest(new(path, new RequestParam(input)));
            if (result != null && !NotProcessed)
            {
                var resultString = JsonConvert.SerializeObject(result, Json.DefaultSettings);
                await response.SendJsonBytesAsync(Encoding.UTF8.GetBytes(resultString));
            }
        }
    }
}
