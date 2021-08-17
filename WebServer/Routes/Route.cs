using System.Threading.Tasks;

namespace UwebServer.Routes
{
    public abstract class Route
    {
        public Method Method {get; set;}
        public string Path { get; set; }
        public abstract Task ProcessAsync(IRequestHeaders headers, Response response);
    }
}