module RequestTypes
open System.IO
open System.Net.Sockets

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
