module Request
open Header
open ResponseData
open Static
open Session
open WebSocket

let startRequesting headerResult configuration requestSession sessionCallback =
    match initialize headerResult with
    | Some header ->
        let requestData = RequestData.create configuration header requestSession
        let responseData = create requestData
        
        async {
            match header.Header "Upgrade" with
            | Some "websocket" -> 
                upgrade header responseData.response.Value requestData.session.networkStream sessionCallback
            | _ ->
                let! processed = sessionCallback.asyncRequest {
                    url = header.url
                    query = responseData.query
                    asyncSendJson = Response.asyncSendJson responseData
                }
                
                if not processed then
                    do! asyncServeStatic requestData
                requestSession.startReceive requestSession configuration
        } |> Async.StartImmediate
    | None -> ()
 