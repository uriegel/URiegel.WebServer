using System;
using System.Threading.Tasks;

namespace UwebServer
{
    public abstract class Route
    {
        public Method Method {get; set;}
        public string Path { get; set; }
        public abstract Task ProcessAsync(Response response);
    }
}