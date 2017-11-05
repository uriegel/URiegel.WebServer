module RequestSession
open System
open System.IO
open System.Net.Sockets

type RequestSession = {
    tcpClient: TcpClient
    networkStream: Stream
}

let private asyncReadHeaders (session: RequestSession) (readBuffer: byte[]) (bufferPosition: int) = 
    async {
        let rec asyncRead = 
            async {
                return "Affe"
            }
        let! headerString = asyncRead
        return headerString
    }

let private close (session: RequestSession) (fullClose: bool) =
    if fullClose then
        session.networkStream.Close ()
    else
        session.tcpClient.Client.Shutdown (SocketShutdown.Send)

let private asyncReceive (session: RequestSession) () = 
    async {
        try 
            let readBuffer = Array.zeroCreate 20000
            let! bufferPosition = session.networkStream.ReadAsync(readBuffer, 0, readBuffer.Length) |> Async.AwaitTask
            if bufferPosition = 0 then
                return false
            else
                let! result = asyncReadHeaders session readBuffer bufferPosition
                return true
        with 
            | :? SocketException as se when se.SocketErrorCode = SocketError.TimedOut -> 
                printfn "Socket session closed, Timeout has occurred"
                close session true
                return false
            // TODO
            // | :? CloseException 
            //     -> socketSession.tcpClient.Close ()
            | :? ObjectDisposedException ->
                close session true
                return false
            | :? IOException ->
                close session true
                return false
            | ex -> 
                printfn "Socket session closed, an error has occurred while receiving: %s" (ex.ToString ())
                close session true
                return false
    }

let create (tcpClient: TcpClient) =
    let session = {
        tcpClient = tcpClient
        networkStream = tcpClient.GetStream ()
    }
    asyncReceive session
