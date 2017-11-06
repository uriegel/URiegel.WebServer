module SocketSession
open System.Net.Sockets
open RequestSession

type SocketSession = {
    tcpClient: TcpClient
    asyncRequestReceive: unit->Async<unit>
}

    // TODO: Keine verschachtelten async-Blöcke
    // let rec retry work resultOk retries = async {
    //   let! res = work
    //   if (resultOk res) || (retries = 0) then return res
    //   else return! retry work resultOk (retries - 1) }

let create (tcpClient: TcpClient) = 
    let socketSession = {
        tcpClient = tcpClient
        asyncRequestReceive = create tcpClient
    }
    socketSession.asyncRequestReceive ()
