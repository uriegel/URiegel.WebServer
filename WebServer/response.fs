module Response
open ResponseData
open System
open System.Text


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