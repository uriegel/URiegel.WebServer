module SocketSession
open System.Net.Sockets

type SocketSession = {
    asyncReceive: Async<unit>
}

let private asyncReceive (tcpClient: TcpClient)= 
    Async.Sleep(2999)

let create (tcpClient: TcpClient) = 
    {
        asyncReceive = asyncReceive tcpClient
    }
