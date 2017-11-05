module SocketSession
open System.IO
open System.Net.Sockets
open System.Security.Authentication
open RequestSession

type SocketSession = {
    tcpClient: TcpClient
    asyncRequestReceive: unit->Async<bool>
}

let private asyncStartReceive (socketSession: SocketSession)() = 
    async {
        let rec asyncReceive () = 
            async  {
                try
                    let! result = socketSession.asyncRequestReceive ()
                    if result then 
                        do! asyncReceive ()
                with 
                | :? AuthenticationException as ae
                    -> printfn "An authentication error has occurred while reading socket, endpoint: %s, error: %s" (socketSession.tcpClient.Client.RemoteEndPoint.ToString()) (ae.ToString ()) 
                | :? IOException 
                    -> socketSession.tcpClient.Close ()
                // TODO
                // | :? CloseException 
                //     -> socketSession.tcpClient.Close ()
                | :? SocketException 
                    -> socketSession.tcpClient.Close ()
                | ex -> printfn "An error has occurred while reading socket: error: %s" (ex.ToString ())
            }
        do! asyncReceive ()
    }

let create (tcpClient: TcpClient) = 
    let socketSession = {
        tcpClient = tcpClient
        asyncRequestReceive = create tcpClient
    }
    asyncStartReceive socketSession
