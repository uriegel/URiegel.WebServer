module Request
open Header
open ResponseData
open Static
open Session
open WebSocket
open System.Text

let asyncGetJson (requestData: RequestData.RequestData) () = 
    //async {
        //let jason = DataContractJsonSerializer (json.GetType())
        //use memStm = new MemoryStream ()
        //let streamToDeserialize = 
        //    match responseData.requestData.header.contentEncoding.Value with
        //    | ContentEncoding.Deflate -> new DeflateStream (memStm, CompressionMode.Compress, true) :> Stream
        //    | ContentEncoding.GZip -> new GZipStream (memStm, CompressionMode.Compress, true) :> Stream
        //    | _ -> memStm :> Stream
        //jason.WriteObject (streamToDeserialize, json)
        //if responseData.requestData.header.contentEncoding.Value <> ContentEncoding.None then 
        //    streamToDeserialize.Close ()

        //memStm.Capacity <- int memStm.Length
        //do! asyncSendJsonBytes responseData <| memStm.GetBuffer ()
      //  do! 
    Encoding.UTF8.GetString (requestData.buffer.buffer, requestData.buffer.currentIndex, requestData.buffer.read - requestData.buffer.currentIndex)

    //"HAllo"
    //} |> Async.StartImmediate


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
                    let! processed = configuration.asyncRequest {
                        url = header.url
                        query = responseData.query
                        asyncSendJson = Response.asyncSendJson responseData
                        asyncGetJson = asyncGetJson requestData
                    }
                    
                    if not processed then
                        do! asyncServeStatic requestData
                requestSession.startReceive requestSession configuration
        } |> Async.StartImmediate
    | None -> ()
 