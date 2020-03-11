module Request
open Header
open Response
open ResponseData
open Static
open Session
open WebSocket
open System.IO

let asyncGetText (requestData: obj) = 
    let requestDataValue = requestData :?> RequestData.RequestData
    let buffer = requestDataValue.buffer
    System.Text.Encoding.UTF8.GetString (buffer.buffer, buffer.currentIndex, buffer.read - buffer.currentIndex)

let asyncGetJson<'T> (requestData: obj) = 
    let requestDataValue = requestData :?> RequestData.RequestData
    let buffer = requestDataValue.buffer
    use memStm = new MemoryStream ( buffer.buffer, buffer.currentIndex, buffer.read - buffer.currentIndex)
    Json.deserializeStream<'T> memStm

let startRequesting headerResult configuration requestSession buffer =
    match initialize headerResult with
    | Some header ->
        let requestData = RequestData.create configuration header requestSession buffer
        let responseData = create requestData
        
        async {
            match header.Header "Upgrade" with
            | Some "websocket" -> 
                upgrade header responseData.response.Value requestData.session.networkStream configuration.onNewWebSocket
            | _ ->
                if configuration.favicon <> "" && header.url = "/favicon.ico" then 
                    do! asyncServeFavicon requestData configuration.favicon
                else
                    if requestData.header.method <> Method.Options then
                        let! processed = configuration.asyncRequest {
                            Url = header.url
                            Method = header.method
                            Query = responseData.query
                            AsyncSendJson = Response.asyncSendJson responseData
                            AsyncSendText = Response.asyncSendText responseData
                            RequestData = requestData
                        }
                    
                        if not processed then
                            do! asyncServeStatic requestData
                    else
                        let responseData = create requestData
                        do! asyncSendOption responseData
                requestSession.startReceive requestSession configuration
        } |> Async.StartImmediate
    | None -> ()
 