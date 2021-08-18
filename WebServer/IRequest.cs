using System.IO;
using System.Threading.Tasks;

namespace UwebServer
{
    public interface IRequest
    {
        Task ReadStreamAsync(Stream stream);
    }
}