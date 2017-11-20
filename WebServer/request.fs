module Request
open Header
open RequestData
open Static

let request headerResult configuration requestSession =
    let header = initialize headerResult
    let requestData = create configuration header requestSession
    
    async {
        if not (configuration.request header.url) then
            do! serveStatic requestData
        requestSession.startReceive (requestSession, configuration) |> ignore
    } |> Async.StartImmediate


    