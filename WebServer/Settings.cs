using System;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using UwebServer.Routes;

namespace UwebServer
{
    public class Settings
    {
        public IPAddress LocalAddress { get; set; } = IPAddress.Any;
        public string ServerString { get; set; } = "UwebServer";
        public TimeSpan SocketTimeout { get; set; }
        public string DomainName { get; set; }
        public int Port { get; set; } = 80;
        public int TlsPort { get; set; } = 443;
        public bool IsTlsEnabled { get; set; }
        public int MinSizeForDeflate { get; set; } = 200;
        public TimeSpan SocketLifetime { get; set; }
        public SslProtocols TlsProtocols { get; set; } = SslProtocols.Tls13;
        public bool CheckRevocation { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public Route[] Routes { get; set; }

        public Settings()
        {
            SocketTimeout = TimeSpan.FromSeconds(20);
            SocketLifetime = TimeSpan.FromMinutes(3);
        }

        public override string ToString()
        => @$"LocalAddress: {LocalAddress}
ServerString: {ServerString}
SocketTimeout: {SocketTimeout}
DomainName: {DomainName}
Port: {Port}
TlsPort: {TlsPort}
IsTlsEnabled: {IsTlsEnabled}
MinSizeForDeflate: {MinSizeForDeflate}
SocketLifetime: {SocketLifetime}
TlsProtocols: {TlsProtocols}
CheckRevocation: {CheckRevocation},
Certificate: {Certificate}";

    }
}