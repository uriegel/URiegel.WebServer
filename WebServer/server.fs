module Server
open System
open System.Net
open System.Net.Sockets
open SocketSession

// TODO: RequestSession mit Headers:
// TODO: async nur bis Daten da, keine Verschachtelten async-Blöcke in der SocketSession
// TODO: Asynchrones rekursives Einlesen, bis entweder read = 0 oder Header-EndIndex gesetzt

// TODO: Dann HEader einlesen synchron

// TODO: async beim Einlesen des Payloads

// TODO: Send Not found
// TODO: TLS

type Server = {
    start: unit->unit
    stop: unit->unit
}

let private asyncOnConnected (tcpClient: TcpClient) = 
    async {
        try
            let asyncReceive = create tcpClient
            do! asyncReceive ()
        with
        | :? SocketException as se when se.NativeErrorCode = 10054
            -> ()
        | :? ObjectDisposedException 
            -> ()
        | ex -> printfn "Error in asyncOnConnected occurred: %s" (ex.ToString ()) 
    }

let private asyncStartConnecting (listener: TcpListener) = 
    async {
        let rec asyncConnect () = 
            async {
                try
                    let! client = listener.AcceptTcpClientAsync () |> Async.AwaitTask
                    asyncOnConnected client |> Async.Start
                    do! asyncConnect ()
                with
                | :? SocketException as se when se.SocketErrorCode = SocketError.Interrupted 
                    -> printfn "Stopping listening..."
                | ex -> printfn "Could not stop HTTP Listener: %s" (ex.ToString ()) 
            }
        do! asyncConnect ()
    } |> Async.Start

let private start (listener: TcpListener) () = 
    try
        printfn "Starting HTTP Listener..."
        listener.Start ()
        asyncStartConnecting listener
        printfn "HTTP Listener started"
    with 
    | ex -> 
        printfn "Could not start HTTP Listener: %s" (ex.ToString ())
        raise ex

let private stop (listener: TcpListener) () = 
    try
        printfn "Stopping HTTP Listener..."
        listener.Stop ()
        printfn "HTTP Listener stopped"
    with 
        | ex -> printfn "Could not stop HTTP Listener: %s" (ex.ToString ())

let create (configuration: Configuration.Value) = 
    printfn "Initializing Server..."
    //ServicePointManager.SecurityProtocol = 10000 |> ignore
    printfn "Domain name: %s" configuration.DomainName
    if configuration.LocalAddress <> IPAddress.Any then 
        printfn "Binding to local address: %s" (configuration.LocalAddress.ToString ())

    printfn "Listening on port %d" configuration.Port
    let result = IPV6ListenerFactory.create configuration.Port
    let listener = result.Listener
    if not result.Ipv6 then 
        printfn "IPv6 or IPv6 dual mode not supported, switching to IPv4"

    printfn "Server initialized"
    {
        start = start listener
        stop = stop listener
    }
    

