module Request
open System
open Header
open ResponseData
open Static
open WebSocket

let startRequesting headerResult configuration requestSession asyncCheckRequest =
    match initialize headerResult with
    | Some header ->
        let requestData = RequestData.create configuration header requestSession
        let responseData = create requestData
        
        async {
            match header.Header "Upgrade" with
            | Some "websocket" -> 
                upgrade header
            | _ ->
                let! processed = asyncCheckRequest header.url responseData
                if not processed then
                    do! asyncServeStatic requestData
                requestSession.startReceive requestSession configuration
        } |> Async.StartImmediate
    | None -> ()
 