using System;

class CertificateException : Exception
{
    public CertificateException(CertificateError error, string message) : base(message) => Error = error;
    public CertificateException(CertificateError error, string message, string[] chainErrorDescriptions) : base(message)
    {
        Error = error;
        ChainErrorDescriptions = chainErrorDescriptions;
    }

    public readonly CertificateError Error;
    public readonly string[] ChainErrorDescriptions = new string[0];
}
