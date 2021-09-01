using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UwebServer.Routes
{
    public class JsonRest : Route
    {
        public Func<UrlComponents, Task<object>> OnRequest { get; set;}
        public JsonRest(string path, Func<UrlComponents, Task<object>> onRequest)
        {
            Method = Method.GET;
            Path = path;
            OnRequest = onRequest;
        } 

        public override async Task<bool> ProcessAsync(IRequest request, IRequestHeaders requestHeaders, Response response)
        {
            UrlComponents query = new UrlComponents(requestHeaders.Url, Path);
            var result = await OnRequest(query);
            if (result != null)
            {
                var resultString = JsonConvert.SerializeObject(result, Json.DefaultSettings);
                await response.SendJsonBytesAsync(Encoding.UTF8.GetBytes(resultString));
                return true;
            }
            else
                return false;
        }
    }
}
