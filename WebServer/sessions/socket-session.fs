module SocketSession
open System.IO
open System.Net.Sockets

type SocketSession = {
    asyncReceive: Async<unit>
}

// TODO: networktstream in SocketSession record
let mutable private networkStream: Stream option = None

let private asyncReceive (tcpClient: TcpClient) = 
    async  {
        if networkStream.IsNone then
            networkStream <- Some (tcpClient.GetStream () :> Stream) 
        do! Async.Sleep(2999) 
    }

let create (tcpClient: TcpClient) = 
    {
        asyncReceive = asyncReceive tcpClient
    }
