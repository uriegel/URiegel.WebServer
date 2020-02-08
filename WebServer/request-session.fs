module RequestSession
open RequestTypes
open Request
open System
open System.IO
open System.Net.Sockets
open System.Security.Authentication
open System.Text

let private close session fullClose =
    if fullClose then
        session.networkStream.Close ()
    else
        session.tcpClient.Client.Shutdown (SocketShutdown.Send)

let private checkHeaders buffer = 
    let index = seq {0..buffer.currentIndex} |> Seq.tryFindIndex (fun i ->
        i > 4 && buffer.buffer.[i] = byte '\n' && buffer.buffer.[i - 1] = byte '\r' && buffer.buffer.[i - 2] = byte '\n')

    match index.IsSome with
    | true ->             
        let resultBuffer = {
            buffer with
                currentIndex = index.Value - 1
        } 
        {
            header = Some (Encoding.ASCII.GetString (buffer.buffer, 0, index.Value - 1))
            buffer = resultBuffer
        } 
    | false ->
        {
            header = None
            buffer = buffer
        } 

let private startReadBuffer buffer action =
    async {
        try 
            let! read = buffer.session.networkStream.AsyncRead (buffer.buffer, buffer.currentIndex, buffer.buffer.Length - buffer.currentIndex)
            if read <> 0 then
                let buffer = {
                    buffer with
                        currentIndex = buffer.currentIndex + read
                        read = read
                }
                action buffer
            else
                ()
        with 
            | :? SocketException as se when se.SocketErrorCode = SocketError.TimedOut -> 
                printfn "Socket session closed, Timeout has occurred"
                close buffer.session true
            | :? SocketException -> 
                close buffer.session true
            | :? ObjectDisposedException ->
                close buffer.session true
            | :? IOException ->
                close buffer.session true
            | :? AuthenticationException as ae -> 
                printfn "An authentication error has occurred while reading socket, endpoint: %s, error: %s" 
                    (buffer.session.tcpClient.Client.RemoteEndPoint.ToString()) (ae.ToString ()) 
            | ex -> 
                printfn "Socket session closed, an error has occurred while receiving: %s" (ex.ToString ())
                close buffer.session true
    } |> Async.StartImmediate

let private startReceive session configuration  =
    let buffer = {
        session = session
        buffer = Array.zeroCreate 20000
        currentIndex = 0
        read = 0
    } 
    startReadBuffer buffer <|fun buffer -> 
        let result = checkHeaders buffer 
        if result.header.IsSome then
            startRequesting result configuration session result.buffer
        else
            startReadBuffer result.buffer |> ignore

let create tcpClient configuration =
    let session = {
        tcpClient = tcpClient
        networkStream = tcpClient.GetStream ()
        startReceive = startReceive
    }
    startReceive session configuration 