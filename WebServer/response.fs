module Response
open ResponseData
open ResponseHeaders
open System
open System.IO
open System.Text
open Header
open System.IO.Compression
open ResponseHeaders

let createHeader responseData (header: Map<string,string>) status statusDescription = 
    let responseHeaders = 
        if not <| header.ContainsKey "Content-Length" then
            header.Add("Connection", "close").
                Add("Date", DateTime.Now.ToUniversalTime().ToString "R").
                Add("Server", "UR Web Server")
        else
            header
        
        // if (server.Configuration.XFrameOptions != XFrameOptions.NotSet)
        //     headers["X-Frame-Options"] = server.Configuration.XFrameOptions.ToString();
    let headerLines = responseHeaders |> Seq.map (fun n -> sprintf "%s: %s" n.Key n.Value)
    let headerString = sprintf "%s %d %s\r\n%s\r\n\r\n" responseData.response.Value status statusDescription <| String.Join ("\r\n", headerLines) 

    // if (payload == null)
    ASCIIEncoding.ASCII.GetBytes(headerString);
        //     else
        //     {
        //         var result = new byte[ASCIIEncoding.ASCII.GetByteCount(headerString) + payload.Length];
        //         var headerBytes = ASCIIEncoding.ASCII.GetBytes(headerString, 0, headerString.Length, result, 0);
        //         Array.Copy(payload, 0, result, headerBytes, payload.Length);
        //         return result;
        //     }

let createHeaderOk responseData header = 
    createHeader responseData header 200 "OK"

let asyncSendError (responseData: ResponseData) htmlHead htmlBody (status: int) (statusDescription: string) = async {
    let response = sprintf "<html><head>%s</head><body>%s</body></html>" htmlHead htmlBody
    let responseBytes = Encoding.UTF8.GetBytes response
   
    let headers = Map.empty.
                    Add("Content-Length", responseBytes.Length.ToString ()).
                    Add("Content-Type", "text/html; charset = UTF-8")

    let headerBytes = createHeader responseData headers status statusDescription                     

    do! responseData.requestData.session.networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
    do! responseData.requestData.session.networkStream.AsyncWrite (responseBytes, 0, responseBytes.Length)
} 

let asyncSendStream (responseData: ResponseData) (stream: Stream) (contentType: string) (lastModified: string) = async {
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

    let headerBytes = createHeaderOk responseData headers 
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