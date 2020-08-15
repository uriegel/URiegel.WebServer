module Response
open ResponseData
open System
open System.IO
open System.Text
open Header
open System.IO.Compression
open ResponseHeaders
open Session
open FSharpTools

let asyncRedirect302 (requestData: RequestData.RequestData) url = async {
    let responseData = create requestData
    let headerLines = 
        requestData.responseHeaders
        |> Seq.map (fun n -> sprintf "%s: %s" n.Key n.Value)

    let payLoad = 
        sprintf "%s 302 Found\r\nLocation: %s\r\nContent-Length: 0\r\n%s\r\n\r\n"
            responseData.response.Value url (headerLines |> String.joinStr "\r\n")
    
    let payLoadBytes = Encoding.UTF8.GetBytes payLoad
    do! responseData.requestData.session.networkStream.AsyncWrite (payLoadBytes, 0, payLoadBytes.Length)
}

let getAllowedOrigin (responseData: ResponseData) =
    match responseData.requestData.configuration.AllowOrigins with
        | Some allowOrigins ->
            match responseData.requestData.header.origin.Value with
            | Some origin -> 
                match responseData.requestData.header.host.Value <> origin with
                | true -> allowOrigins |> Seq.tryFind (fun n -> String.CompareOrdinal (n, origin) = 0) 
                | false -> None
            | _ -> None
        | _ -> None

let createHeader responseData (header: Map<string,string>) status statusDescription (payload: byte[] option) = 
    let responseHeaders = ResponseHeaders.initialize header "" None
    let responseHeaders = 
        match getAllowedOrigin responseData with
        | Some originToAllow -> responseHeaders.Add("Access-Control-Allow-Origin", originToAllow)
        | None -> responseHeaders

    // if (server.Configuration.XFrameOptions != XFrameOptions.NotSet)
    //     headers["X-Frame-Options"] = server.Configuration.XFrameOptions.ToString();

    let headerLines = responseHeaders |> Seq.map (fun n -> sprintf "%s: %s" n.Key n.Value)
    let headerString = sprintf "%s %d %s\r\n%s\r\n\r\n" responseData.response.Value status statusDescription <| String.Join ("\r\n", headerLines) 

    if payload.IsNone then
        ASCIIEncoding.ASCII.GetBytes(headerString)
    else 
        let result = Array.zeroCreate (ASCIIEncoding.ASCII.GetByteCount headerString + payload.Value.Length)
        let headerBytes = ASCIIEncoding.ASCII.GetBytes (headerString, 0, headerString.Length, result, 0)
        Array.Copy (payload.Value, 0, result, headerBytes, payload.Value.Length)
        result

let createHeaderOk responseData header payload = 
    createHeader responseData header 200 "OK" payload

let asyncSendOption responseData = 
    async {
        let responseHeaders = Map.empty    
        let responseHeaders = 
            match getAllowedOrigin responseData with
            | Some _ -> 
                let responseHeaders = 
                    match responseData.requestData.header.rawHeaders.TryFind "Access-Control-Request-Headers" with
                    | Some value -> responseHeaders.Add("Access-Control-Allow-Headers", value)
                    | None -> responseHeaders    
                let responseHeaders = 
                    match responseData.requestData.header.rawHeaders.TryFind "Access-Control-Request-Method" with
                    | Some value -> responseHeaders.Add("Access-Control-Allow-Method", value)
                    | None -> responseHeaders    
                responseHeaders
            | None -> responseHeaders
        
        let bytes = createHeader responseData responseHeaders 204 "No Content" None
        do! responseData.requestData.session.networkStream.AsyncWrite (bytes, 0, bytes.Length)
    } 

let asyncSendError responseData htmlHead htmlBody status statusDescription = 
    async {
        let response = sprintf "<html><head>%s</head><body>%s</body></html>" htmlHead htmlBody
        let responseBytes = Encoding.UTF8.GetBytes response
       
        let headers = Map.empty.
                        Add("Content-Length", responseBytes.Length.ToString ()).
                        Add("Content-Type", "text/html; charset = UTF-8")

        let bytes = createHeader responseData headers status statusDescription (Some responseBytes)
        do! responseData.requestData.session.networkStream.AsyncWrite (bytes, 0, bytes.Length)
    } 

let private asyncSendJsonBytes responseData (bytes: byte[]) = 
    async {
        let mutable contentLength = bytes.Length
        let headers = Map.empty.
                        Add("Content-Length", contentLength.ToString ()).
                        Add("Content-Type", "application/json; charset=UTF-8").
                        Add("Cache-Control", "no-cache,no-store")
        let headers =
            match responseData.requestData.header.contentEncoding.Value with
            | ContentEncoding.Deflate -> headers.Add("Content-Encoding", "deflate")
            | ContentEncoding.GZip -> headers.Add("Content-Encoding", "gzip")
            | _ -> headers    

        let bytes = createHeaderOk responseData headers (Some bytes)
        do! responseData.requestData.session.networkStream.AsyncWrite (bytes, 0, bytes.Length) 
    }

let asyncSendJson responseData json = 
    async {
        use memStm = new MemoryStream ()
        let streamToDeserialize = 
            match responseData.requestData.header.contentEncoding.Value with
            | ContentEncoding.Deflate -> new DeflateStream (memStm, CompressionMode.Compress, true) :> Stream
            | ContentEncoding.GZip -> new GZipStream (memStm, CompressionMode.Compress, true) :> Stream
            | _ -> memStm :> Stream

        Json.serializeStream streamToDeserialize json 
        if responseData.requestData.header.contentEncoding.Value <> ContentEncoding.None then 
            streamToDeserialize.Close ()

        memStm.Capacity <- int memStm.Length
        do! asyncSendJsonBytes responseData <| memStm.GetBuffer ()
    }

let asyncCopyStream (source: Stream) (target: Stream) bufferSize = async {
    let buffer = Array.zeroCreate bufferSize
    
    let rec copy () = async {
        let! read = source.AsyncRead (buffer, 0, buffer.Length)
        do! target.AsyncWrite (buffer, 0, read)
        match read with
        | 0 -> ()
        | _ -> do! copy ()
    }
    do! copy ()
}   

let asyncSendRaw responseData status description stream = 
    async {
        let headers = 
            responseData.requestData.responseHeaders
            |> Seq.map (|KeyValue|)
            |> Map.ofSeq

        let bytes = createHeader responseData headers status description None
        do! responseData.requestData.session.networkStream.AsyncWrite (bytes, 0, bytes.Length) 
        do! asyncCopyStream stream responseData.requestData.session.networkStream 20000
        responseData.requestData.session.networkStream.Close ()
    }

let asyncSendText responseData (text: string) = 
    async {
        let bytes = Encoding.UTF8.GetBytes text

        use memStm = new MemoryStream ()
        let streamToDeserialize = 
            match responseData.requestData.header.contentEncoding.Value with
            | ContentEncoding.Deflate -> new DeflateStream (memStm, CompressionMode.Compress, true) :> Stream
            | ContentEncoding.GZip -> new GZipStream (memStm, CompressionMode.Compress, true) :> Stream
            | _ -> memStm :> Stream

        streamToDeserialize.Write (bytes, 0, bytes.Length)
        if responseData.requestData.header.contentEncoding.Value <> ContentEncoding.None then 
            streamToDeserialize.Close ()

        memStm.Capacity <- int memStm.Length
        let bytes = memStm.GetBuffer ()
        let mutable contentLength = bytes.Length
        let headers = Map.empty.
                        Add("Content-Length", contentLength.ToString ()).
                        Add("Content-Type", "text/plain; charset=UTF-8").
                        Add("Cache-Control", "no-cache,no-store")
        let headers =
            match responseData.requestData.header.contentEncoding.Value with
            | ContentEncoding.Deflate -> headers.Add("Content-Encoding", "deflate")
            | ContentEncoding.GZip -> headers.Add("Content-Encoding", "gzip")
            | _ -> headers    

        let bytes = createHeaderOk responseData headers (Some bytes)
        do! responseData.requestData.session.networkStream.AsyncWrite (bytes, 0, bytes.Length) 
    }

let asyncSendNotFound responseData = async {
    do! asyncSendError responseData @"<title>CAESAR</title>
<Style> 
html {
    font-family: sans-serif;
}
h1 {
    font-weight: 100;
}
</Style>" "<h1>Datei nicht gefunden</h1><p>Die angegebene Resource konnte auf dem Server nicht gefunden werden.</p>" 404 "Not Found"
} 

let asyncSend304 responseData = async {
    let headerString = sprintf "%s 304 Not Modified\r\n\r\n" responseData.response.Value
    let headerBytes = ASCIIEncoding.ASCII.GetBytes headerString
    do! responseData.requestData.session.networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
}
