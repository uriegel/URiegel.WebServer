module WebSocket
open Header
open System.Security.Cryptography
open System.Text
open System
open System.IO
open WebSocketSession
open Session

let private webSocketKeyConcat = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"

let asyncSendMessage () = ()

let upgrade (header: Header) httpResponse (networkStream: Stream) (sessionCallback: SessionCallback) = 
    match header.Header "Sec-WebSocket-Key" with
    | Some key ->
        let secKey = key + webSocketKeyConcat
        let hashKey = (SHA1.Create ()).ComputeHash (Encoding.UTF8.GetBytes secKey)
        let response = sprintf "%s 101 Switching Protocols\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Accept: %s\r\n\r\n" httpResponse 
                        <| Convert.ToBase64String hashKey
        let headerBytes = ASCIIEncoding.ASCII.GetBytes response
        async {
            do! networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
            let callback = sessionCallback.onNewWebSocket "TODO" { asyncSendMessage = asyncSendMessage } 
            startReceiving networkStream callback
            ()
        } |> Async.StartImmediate
    | None -> ()
