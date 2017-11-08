module RequestTypes
open System.IO
open System.Net.Sockets
open System

type RequestSession = {
    tcpClient: TcpClient
    networkStream: Stream
    startReceive: (RequestSession * Configuration.Value) -> IAsyncResult
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
