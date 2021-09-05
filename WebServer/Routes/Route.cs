using System.Threading.Tasks;

namespace UwebServer.Routes
{
    public abstract class Route
    {
        public Method Method {get; set;}
        public string Path { get; set; }
        public string Host { get; set; }
        public bool? Tls { get; set; }
        public BasicAuthentication BasicAuthentication { get; set; }
        public abstract Task<bool> ProcessAsync(IRequest request, IRequestHeaders headers, Response response);
    }
}