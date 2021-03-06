using System.IO;
using System.Threading.Tasks;

namespace UwebServer
{
    public interface IRequest
    {
        string Id { get; }
        string HttpResponseString { get; }
        Task ReadStreamAsync(Stream stream);
        bool ConnectionClose { get; set; }
    }

    internal interface IInternalRequest
    {
        Task WriteAsync(byte[] buffer, int offset, int length);
    }
}