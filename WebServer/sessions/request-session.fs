module RequestSession
open System
open System.IO
open System.Net.Sockets
open System.Security.Authentication
open System.Text

type RequestSession = {
    tcpClient: TcpClient
    networkStream: Stream
}

type Buffer = {
    session: RequestSession
    buffer: byte[]
    currentIndex: int
    read: int
}

type HeaderResult = {
    buffer: Buffer
    header: string
}

let private close (session: RequestSession) (fullClose: bool) =
    if fullClose then
        session.networkStream.Close ()
    else
        session.tcpClient.Client.Shutdown (SocketShutdown.Send)

let private asyncReadBuffer (buffer: Buffer) =
    async {
        let! read = buffer.session.networkStream.ReadAsync(buffer.buffer, buffer.currentIndex, buffer.buffer.Length - buffer.currentIndex) |> Async.AwaitTask
        return {
            buffer with
                currentIndex = buffer.currentIndex + read
                read = read
        }
    }

let rec private asyncReadHeaders (buffer: Buffer) = 
    async {
        let index = seq {0..buffer.currentIndex} |> Seq.tryFindIndex (fun i ->
            i > 4 && buffer.buffer.[i] = byte '\n' && buffer.buffer.[i - 1] = byte '\r' && buffer.buffer.[i - 2] = byte '\n')

        let result = async {
            match index.IsSome with
            | true ->             
                let! result = async.Return {
                    header = Encoding.ASCII.GetString (buffer.buffer, 0, index.Value - 1)
                    buffer = buffer
                } 
                return result
            | false ->
                let! buffer = asyncReadBuffer buffer
                // TODO: Keine Exception
                if buffer.read = 0 then 
                    failwith "Affe"

                let! result = asyncReadHeaders buffer
                return result
        }
        let! result = result
        return result
    }

let rec private asyncReceive (session: RequestSession)() =
    async {
        try 
            let buffer1 = {
                session = session
                buffer = Array.zeroCreate 20000
                currentIndex = 0
                read = 0
            } 
            let! buffer = asyncReadBuffer buffer1
            if buffer.read = 0 then
                return! asyncReceive () 
            // if buffer.read = 0 then
            //     return
            // else
            //     printf "Suppa 2"
            //     let! result = asyncReadHeaders buffer
            //     printf "Suppa"
            //     let header = result.header
            //     let affe = header
            //     return
        with 
            | :? SocketException as se when se.SocketErrorCode = SocketError.TimedOut -> 
                printfn "Socket session closed, Timeout has occurred"
                close session true
                return false
            | :? SocketException -> 
                close session true
                return false
            // TODO: Keine Exception
            // | :? CloseException 
            //     -> socketSession.tcpClient.Close ()
            | :? ObjectDisposedException ->
                close session true
                return false
            | :? IOException ->
                close session true
                return false
            | :? AuthenticationException as ae -> 
                printfn "An authentication error has occurred while reading socket, endpoint: %s, error: %s" (socketSession.tcpClient.Client.RemoteEndPoint.ToString()) (ae.ToString ()) 
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
