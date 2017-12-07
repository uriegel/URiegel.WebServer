module Configuration
open System
open System.IO
open System.Net
open Session
open System.Threading.Tasks

type Value = {
    LocalAddress: IPAddress
    DomainName: string
    WebRoot: string
    SocketTimeout: int
    Port: int
    IsTlsEnabled: bool 
    favicon: string
    asyncRequest: RequestSession->Async<bool>
    onNewWebSocket: string->WebSocketSession->WebSocketCallback
}

let createEmpty () = {
    LocalAddress = IPAddress.Any
    DomainName = ""
    WebRoot = ""
    SocketTimeout = 20000
    Port = 80
    IsTlsEnabled = false
    favicon = ""
    asyncRequest = fun _ -> Task.FromResult<bool>(false) |> Async.AwaitTask
    onNewWebSocket = fun _ __ -> { id = ""; onClose = fun _ -> () }
}

let create configuration = {
    configuration with 
        WebRoot = if configuration.WebRoot = "" then Directory.GetCurrentDirectory () else configuration.WebRoot
        SocketTimeout = if configuration.SocketTimeout = 0 then 20000 else configuration.SocketTimeout
        Port =  if configuration.Port = 0 then 80 else configuration.Port
        DomainName = if configuration.DomainName = "" then (Dns.GetHostEntry(Environment.MachineName)).HostName else configuration.DomainName
}