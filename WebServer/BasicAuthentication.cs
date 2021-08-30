using System;
using System.Text;
using System.Threading.Tasks;

namespace UwebServer
{
    public class BasicAuthentication
    {
        public string Realm { get; set; }

        internal async Task<bool> Authenticate(Response response, IRequestHeaders headers)
        {
            var authorization = headers["Authorization"];
            if (authorization == null)
            {
                await response.Send401Basic(Realm);
                return false;
            }
            else
            {
                var base64 = authorization[6..];
                var auth = Convert.FromBase64String(base64);
                var text = Encoding.UTF8.GetString(auth);


                await Task.Delay(5000);
                await response.Send401Basic(Realm);
                return false;
            }
        }
    }
}