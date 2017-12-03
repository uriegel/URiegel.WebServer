module WebSocketSession
open System.IO
open OpCode
open Session

let asyncMessageReceived (headerBuffer: byte[]) callback = 
    async {
        //int read = 2;
        //let fin = (headerBuffer.[0] &&& 0x80uy = 0x80uy)
        let opcode = enum<OpCode>(int (headerBuffer.[0] &&& 0xfuy))
        match opcode with
        | OpCode.Close -> callback.onClose callback.id
        | _ -> ()
    }

let startReceiving (networkStream: Stream) callback = 
    async {
        let headerBuffer = Array.zeroCreate 14
        let! read = networkStream.AsyncRead (headerBuffer, 0,  2)
        if read = 0 then
            failwith "closed"
        do! asyncMessageReceived headerBuffer callback
    } |> Async.StartImmediate