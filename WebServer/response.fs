module Response
open ResponseData
open System
open System.IO
open System.Text
open Header
open System.IO.Compression
open ResponseHeaders
open Session

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
    let responseHeaders = 
        let header =
            if not <| header.ContainsKey "Content-Length" then
                header.Add("Connection", "close").
                    Add("Date", DateTime.Now.ToUniversalTime().ToString "R").
                    Add("Server", "UR Web Server")
            else
                header

        match getAllowedOrigin responseData with
        | Some originToAllow -> header.Add("Access-Control-Allow-Origin", originToAllow)
        | None -> header

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

let asyncSendStream responseData (stream: Stream) (contentType: string) lastModified = 
    async {
        let mutable headers = Map.empty
        let mutable streamToSend = stream
        if responseData.requestData.header.contentEncoding.Value <> ContentEncoding.None &&
            (contentType.StartsWith ("application/javascript", StringComparison.CurrentCultureIgnoreCase)            
            || contentType.StartsWith ("text/", StringComparison.CurrentCultureIgnoreCase)) then
            
            let ms = new MemoryStream ()
            use compressedStream = 
                match responseData.requestData.header.contentEncoding.Value with    
                | ContentEncoding.Deflate ->
                    headers <- headers.Add("Content-Encoding", "deflate") 
                    new DeflateStream (ms, CompressionMode.Compress, true) :> Stream
                | ContentEncoding.GZip ->
                    headers <- headers.Add("Content-Encoding", "gzip")
                    new GZipStream (ms, CompressionMode.Compress, true) :> Stream
                | _ -> null

            do! stream.CopyToAsync compressedStream |> Async.AwaitTask
            compressedStream.Close();
            ms.Position <- 0L 
            
            streamToSend <- ms

        headers <- initialize headers contentType (int streamToSend.Length) lastModified false

        if contentType.StartsWith ("application/javascript", StringComparison.CurrentCultureIgnoreCase) 
            || contentType.StartsWith ("text/css", StringComparison.CurrentCultureIgnoreCase)
            || contentType.StartsWith ("text/html", StringComparison.CurrentCultureIgnoreCase) then
                headers <- headers.Add("Expires", DateTime.Now.ToUniversalTime().ToString "r")

        let headerBytes = createHeaderOk responseData headers None
        do! responseData.requestData.session.networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
        
        if responseData.requestData.header.method <> Method.Head then
            let bytes = Array.zeroCreate 8192

            let mutable dataToSend = true
            while dataToSend do 
                let! read = streamToSend.AsyncRead (bytes, 0, bytes.Length)
                if read <> 0 then
                    do! responseData.requestData.session.networkStream.AsyncWrite (bytes, 0, read)
                else
                    dataToSend <- false
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
