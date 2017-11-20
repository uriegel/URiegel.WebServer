module Configuration
open System
open System.IO
open System.Net

type Value = {
    LocalAddress: IPAddress
    DomainName: string
    WebRoot: string
    SocketTimeout: int
    Port: int
    IsTlsEnabled: bool 
    request: string->bool
}

let createEmpty () = {
    LocalAddress = IPAddress.Any
    DomainName = ""
    WebRoot = ""
    SocketTimeout = 20000
    Port = 80
    IsTlsEnabled = false
    request = fun _ -> false
}

let create configuration = {
    configuration with 
        WebRoot = if configuration.WebRoot = "" then Directory.GetCurrentDirectory () else configuration.WebRoot
        SocketTimeout = if configuration.SocketTimeout = 0 then 20000 else configuration.SocketTimeout
        Port =  if configuration.Port = 0 then 80 else configuration.Port
        DomainName = if configuration.DomainName = "" then (Dns.GetHostEntry(Environment.MachineName)).HostName else configuration.DomainName
}