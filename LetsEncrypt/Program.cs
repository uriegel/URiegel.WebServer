using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;

namespace LetsEncrypt
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            AcmeContext acme = null;
            IAccountContext account = null;
            if (File.Exists("account.txt"))
            {
                var pemKey = await File.ReadAllTextAsync("account.txt");
                var accountKey = KeyFactory.FromPem(pemKey);
                acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2, accountKey);
                account = await acme.Account();                
            }
            else
            {
                acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
                account = await acme.NewAccount("uriegel@hotmail.de", true);
                //account = await acme.NewAccount("uwriegel@gmail.com", true);
                // Save the account key for later use
                var pemKey = acme.AccountKey.ToPem();
                await File.WriteAllTextAsync("account.txt", pemKey);
            }
            //var order = await acme.NewOrder(new[] { "uriegel.de", "fritz.uriegel.de", "familie.uriegel.de" });
            var order = await acme.NewOrder(new[] { "uriegel.de" });
            //var order = await acme.NewOrder(new[] { "fritz.uriegel.de" });

            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();
            var keyAuthz = httpChallenge.KeyAuthz;
            var token = httpChallenge.Token;

            await File.WriteAllTextAsync("webroot/letsencrypt", keyAuthz);
            try {
                var challenge = await httpChallenge.Validate();
                Console.WriteLine($"Challenge: {challenge.Error}, {challenge.Status} {challenge.Validated}"); 

                Thread.Sleep(5000);

                challenge = await httpChallenge.Validate();
                Console.WriteLine($"Challenge: {challenge.Error}, {challenge.Status} {challenge.Validated}"); 


                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var cert = await order.Generate(new CsrInfo
                {
                    CountryName = "DE",
                    State = "NRW",
                    Locality = "Cologne",
                    Organization = "URiegel",
                    OrganizationUnit = "Dev",
                    CommonName = "uriegel.de",
                }, privateKey);
                
                var certPem = cert.ToPem();
                var pfxBuilder = cert.ToPfx(privateKey);
                var pfx = pfxBuilder.Build("uriegel.de", "uriegel");

                File.WriteAllBytes("zertifikat.pfx", pfx);
                var test = 0;
            }
            catch (Exception e)
            {
                var test = e;
            }
        }
    }
}
