module Request
open Header
open ResponseData
open Static

let request headerResult configuration requestSession checkRequest =
    let header = initialize headerResult
    let requestData = RequestData.create configuration header requestSession
    let responseData = create requestData
    
    async {
        let! processed = checkRequest header.url responseData
        if not processed then
            do! serveStatic requestData
        requestSession.startReceive requestSession configuration |> ignore
    } |> Async.StartImmediate


    