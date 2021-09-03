using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

static class X509CertificateExtensions
{
    public static bool Validate(this X509Certificate certificate, string host, X509Chain chain, SslPolicyErrors sslPolicyErrors, Func<CertificateException, bool> check)
    {
        if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) == SslPolicyErrors.RemoteCertificateChainErrors)
        {
            var chainDescriptions = chain.ChainStatus != null ? chain.ChainStatus.Select(n => $"Chain certificate error: {n.Status} {n.StatusInformation}").ToArray() : null;
            var e = new CertificateException(CertificateError.Chain, "Remote certificate chain error", chainDescriptions ?? new string[0]);
            return check?.Invoke(e) ?? false;
        }
        if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
        {
            var e = new CertificateException(CertificateError.RemoteCertificateNotAvailable, "Remote certificate not available");
            return check?.Invoke(e) ?? false;
        }
        if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) == SslPolicyErrors.RemoteCertificateNameMismatch)
        {
            var alternativeNamesExtension = (certificate as X509Certificate2)?.Extensions.AsEnumerable().FirstOrDefault(n => n.Oid.Value == "2.5.29.17"); // SAN OID
            if (alternativeNamesExtension != null)
            {
                var alts = alternativeNamesExtension.Format(false).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                var result = alts.Any(n => n.EndsWith(host, StringComparison.InvariantCultureIgnoreCase));
                if (result)
                    return true;
                else
                {
                    var e = new CertificateException(CertificateError.NameMismatch, "Remote certificate name mismatch");
                    return check?.Invoke(e) ?? false;
                }
            }
        }
        return true;
    }
}
