module RequestTypes
open System.IO
open System.Net.Sockets

type RawRequestSession = {
    tcpClient: TcpClient
    networkStream: Stream
    mutable startReceive: RawRequestSession->Configuration.Value->bool->unit
}

type Buffer = {
    session: RawRequestSession
    buffer: byte[]
    currentIndex: int
    read: int
}

type HeaderResult = {
    buffer: Buffer
    header: string option
}
