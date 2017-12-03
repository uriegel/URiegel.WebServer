module WebSocketSession
open System.IO
open OpCode
 
let asyncMessageReceived (headerBuffer: byte[]) = 
    async {
        //int read = 2;
        let fin = (headerBuffer.[0] &&& 0x80uy = 0x80uy)
        printf "%A" fin
        let opcode = enum<OpCode>(int (headerBuffer.[0] &&& 0xfuy))
        printf "%O" opcode
    }

let startReceiving (networkStream: Stream) = 
    async {
        // onNewWebSocket  {
        //     onClosed = 
        // }
        let headerBuffer = Array.zeroCreate 14
        let! read = networkStream.AsyncRead (headerBuffer, 0,  2)
        if read = 0 then
            failwith "closed"
        do! asyncMessageReceived headerBuffer
    } |> Async.StartImmediate