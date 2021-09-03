using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UwebServer.Routes
{
    public class LetsEncrypt : Route
    {
        public LetsEncrypt()
        {
            Method = Method.GET;
            Path = "/.well-known/acme-challenge";
            Tls = false;
        }

        public override async Task<bool> ProcessAsync(IRequest request, IRequestHeaders headers, Response response)
        {
            var token = File.ReadAllText(System.IO.Path.Combine(encryptDirectory, "token"));
            Console.WriteLine($"Validating LetsEncrypt token : {token}");
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(token));
            await response.SendStreamAsync(ms, null, null, true);
            return true;
        }   

#if DEBUG
    string encryptDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "letsencrypt-uweb");
#else
    const string encryptDirectory = "/etc/letsencrypt-uweb";
#endif
    }
}