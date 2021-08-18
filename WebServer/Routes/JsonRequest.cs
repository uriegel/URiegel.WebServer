using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UwebServer.Routes
{
    public interface IRequestParam
    {
        T Get<T>();
        JToken Get();
    }

    class RequestParam : IRequestParam
    {
        internal RequestParam(MemoryStream input) => this.input = input;

        public T Get<T>()
        {
            input.Capacity = (int)input.Length;
            var json = Encoding.UTF8.GetString(input.GetBuffer());
            return JsonConvert.DeserializeObject<T>(json);
        }

        public JToken Get()
        {
            input.Position = 0;
            return JToken.ReadFrom(new JsonTextReader(new StreamReader(input)));
        }

        readonly MemoryStream input;
    }
}