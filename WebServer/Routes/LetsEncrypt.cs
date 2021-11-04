using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UwebServer.Routes
{
    static class LetsEncrypt
    {
        public static async Task<bool> CheckProcessAsync(Settings settings, bool isSecure, IRequestHeaders headers, Response response) {
            if (settings.UseLetsEncrypt && !isSecure && headers.Url.StartsWith("/.well-known/acme-challenge", true, null))
            {
                var token = File.ReadAllText(System.IO.Path.Combine(Server.EncryptDirectory, "token"));
                Console.WriteLine($"Validating LetsEncrypt token : {token}");
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(token));
                await response.SendStreamAsync(ms, null, null, true);
                return true;
            }
            else
                return false;
        }
    }
}