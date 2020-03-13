module Server
open System
open System.Net
open System.Net.Sockets
open ResponseData
open Session
open System.Security.Cryptography.X509Certificates

// TODO: TLS

// TODO: async nur bis Daten da, keine Verschachtelten async-Blöcke in der SocketSession
// TODO: Asynchrones rekursives Einlesen, bis entweder read = 0 oder Header-EndIndex gesetzt

type Server = {
    start: unit->unit
    stop: unit->unit
    configuration: Configuration.Value
}
let private onConnected tcpClient configuration (certificate: X509Certificate2 option) = 
    try
        RequestSession.create tcpClient configuration certificate
    with
    | :? SocketException as se when se.NativeErrorCode = 10054
        -> ()
    | :? ObjectDisposedException 
        -> ()
    | ex -> printfn "Error in asyncOnConnected occurred: %s" <| ex.ToString () 


let rec startConnecting (listener: TcpListener) configuration (certificate: X509Certificate2 option) = 
    async {
        try
            let! client = listener.AcceptTcpClientAsync () |> Async.AwaitTask
            //client.NoDelay <- true
            onConnected client configuration certificate
            startConnecting listener configuration certificate
        with
        | :? SocketException as se when se.SocketErrorCode = SocketError.Interrupted 
            -> printfn "Stopping listening..."
        | ex -> printfn "Could not stop HTTP Listener: %s" <|ex.ToString () 
    } |> Async.StartImmediate

let private start (listener: TcpListener) (tlsListener: TcpListener option) (configuration: Configuration.Value) () = 
    try
        printfn "Starting HTTP Listener..."
        // TODO: Ansonsten kann nach Beenden des Listeners für 2 min kein neuer gestartet werden!
        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)    
        listener.Start ()
        startConnecting listener configuration None
        printfn "HTTP Listener started"

        match tlsListener with
        | Some tlsListener ->
            //let certificate = new X509Certificate2("/opt/cert.pfx", "uriegel")
            let certificate = new X509Certificate2("zertifikat.pfx", "uriegel")
            printfn "Using certificate: %O" certificate
            printfn "Starting HTTPS Listener..."
            tlsListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true)    
            tlsListener.Start ()
            startConnecting tlsListener configuration (Some certificate)
            printfn "HTTPS Listener started"
        | None -> ()
    with 
    | ex -> 
        printfn "Could not start HTTP Listener: %s" <|ex.ToString ()
        raise ex

let private stop (listener: TcpListener) (tlsListener: TcpListener option) () = 
    try
        printfn "Stopping HTTP Listener..."
        listener.Stop ()
        printfn "HTTP Listener stopped"
        
        match tlsListener with
        | Some tlsListener -> 
            printfn "Stopping HTTPS Listener..."
            tlsListener.Stop ()
            printfn "HTTPs Listener stopped"
        | None -> ()
    with 
        | ex -> printfn "Could not stop HTTP Listener: %s" <|ex.ToString ()

let create (configuration: Configuration.Value) = 
    printfn "Initializing Server..."
    //ServicePointManager.SecurityProtocol = 10000 |> ignore
    printfn "Domain name: %s" configuration.DomainName
    if configuration.LocalAddress <> IPAddress.Any then 
        printfn "Binding to local address: %s" <| configuration.LocalAddress.ToString ()

    printfn "Listening on port %d" configuration.Port
    let result = Ipv6Listener.Create configuration.Port
    let listener = result.Listener
    if not result.Ipv6 then 
        printfn "IPv6 or IPv6 dual mode not supported, switching to IPv4"

    let tlsListener = 
        if configuration.UseLetsEncrypt then
            printfn "Listening on secure port %d" configuration.TlsPort
            let result = Ipv6Listener.Create configuration.TlsPort
            let tlsListener = result.Listener
            if not result.Ipv6 then 
                printfn "IPv6 or IPv6 dual mode not supported, switching to IPv4"
            Some tlsListener
        else
            None  

    printfn "Server initialized"
    
    {
        start = start listener tlsListener configuration
        stop = stop listener tlsListener
        configuration = configuration
    }

    // TODO: In Webserver, in letsencrypt mode don't call request in unsecure, redirect to 443 or serve acme
    // TODO: New project: LetsEncrypt server to get or renew the certificate
    // TODO: Save certificate in CertificateStore of root
    // TODO: Redirection (Fritz.uriegel.de)
    // TODO: Redirect Reitbeteiligung to Reitbeteiligung/

    

