using System;
using System.Net;

static class HttpWebRequestExtensions
{
    public static void CertificateValidator(this HttpWebRequest webRequest, Func<Exception, bool> check)
        => webRequest.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            certificate.Validate(webRequest.RequestUri.Host, chain, sslPolicyErrors, ce => check(new Exception(ce)));

    public class Exception : System.Exception
    {
        public Exception(CertificateException ce): base(ce.Message)
        {
            switch (ce.Error)
            {
                case CertificateError.Chain:
                    Error = CertificateError.Chain;
                    break;
                case CertificateError.RemoteCertificateNotAvailable:
                    Error = CertificateError.RemoteCertificateNotAvailable;
                    break;
                case CertificateError.NameMismatch:
                    Error = CertificateError.NameMismatch;
                    break;
                default:
                    break;
            }
            ChainErrorDescriptions = ce.ChainErrorDescriptions;
        }

        public readonly CertificateError Error;
        public readonly string[] ChainErrorDescriptions;
    }
}
