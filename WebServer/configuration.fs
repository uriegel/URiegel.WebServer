module Configuration
open System
open System.IO
open System.Net
open Session
open System.Threading.Tasks

type Value = {
    LocalAddress: IPAddress
    DomainName: string
    SocketTimeout: int
    Port: int
    TlsPort: int
    UseLetsEncrypt: bool 
    AllowOrigins: string array option
    RawRequests: (RequestSession->Async<bool>) list
    Requests: (RequestSession->Async<bool>) list
}

let createEmpty () = {
    LocalAddress = IPAddress.Any
    DomainName = ""
    SocketTimeout = 20000
    Port = 80
    TlsPort = 443
    UseLetsEncrypt = false
    AllowOrigins = None
    RawRequests = []
    Requests = []
}

let create configuration = {
    configuration with 
        SocketTimeout = if configuration.SocketTimeout = 0 then 20000 else configuration.SocketTimeout
        Port =  if configuration.Port = 0 then 80 else configuration.Port
        TlsPort =  if configuration.TlsPort = 0 then 443 else configuration.TlsPort
        DomainName = if configuration.DomainName = "" then (Dns.GetHostEntry(Environment.MachineName)).HostName else configuration.DomainName
        AllowOrigins = configuration.AllowOrigins
}