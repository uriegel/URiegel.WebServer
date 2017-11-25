module Request
open Header
open ResponseData
open Static

let startRequesting headerResult configuration requestSession asyncCheckRequest =
    let header = initialize headerResult
    let requestData = RequestData.create configuration header requestSession
    let responseData = create requestData
    
    async {
        let! processed = asyncCheckRequest header.url responseData
        if not processed then
            do! asyncServeStatic requestData
        requestSession.startReceive requestSession configuration
    } |> Async.StartImmediate


    