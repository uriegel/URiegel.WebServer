module Request
open Header
open Response
open ResponseData
open Static
open Session
open WebSocket
open System.IO
open System.Text

let getJson<'T> (requestSession: RequestSession) = 
    let requestDataValue = requestSession.RequestData :?> RequestData.RequestData
    let buffer = requestDataValue.buffer
    use memStm = new MemoryStream ( buffer.buffer, buffer.currentIndex, buffer.read - buffer.currentIndex)
    Json.deserializeStream<'T> memStm

let private asyncValidateLetsEncrypt (responseData: ResponseData) = async {
    let token = File.ReadAllText("/etc/letsencrypt-uweb/token")
    printfn "Validateing LetsEncrypt token : %s" token
    do! asyncSendText responseData token
}   

let private asyncTlsRedirect (requestData: RequestData.RequestData) = async {
    let response = "<html><head>Moved permanently</head><body><h1>Moved permanently</h1>The specified resource moved permanently.</body</html>"
    let responseBytes = Encoding.UTF8.GetBytes response
    let redirectHeaders = 
        sprintf "HTTP/1.1 301 Moved Permanently\r\nLocation: %s%s\r\nContent-Length: %d\r\n\r\n"
            requestData.urlRoot.Value requestData.header.url responseBytes.Length
    let headerBytes = Encoding.UTF8.GetBytes redirectHeaders 

    do! requestData.session.networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
    do! requestData.session.networkStream.AsyncWrite (responseBytes, 0, responseBytes.Length)
}

let startRequesting headerResult configuration requestSession buffer redirectTls =
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
                elif redirectTls then
                    if header.url |> String.startsWith "/.well-known/acme-challenge" then
                        do! asyncValidateLetsEncrypt responseData
                    else
                        do! asyncTlsRedirect requestData
                else
                    if requestData.header.method <> Method.Options then
                        let getText (requestData: RequestData.RequestData) () = 
                            let buffer = requestData.buffer
                            System.Text.Encoding.UTF8.GetString (buffer.buffer, buffer.currentIndex, buffer.read - buffer.currentIndex)
                        let getCookie (requestData: RequestData.RequestData) cookie = 
                            match requestData.header.Header "Cookie" with
                            | Some cookieString -> 
                                let cookies = cookieString |> String.splitChar ';'
                                match cookies |> Seq.tryFind (fun n -> n |> String.startsWith cookie) with
                                | Some value -> 
                                    let pos = value|> String.indexOfChar '='
                                    match pos with
                                    | Some pos -> Some (value |> String.substring (pos + 1))
                                    | None -> None
                                | None -> None
                            | None -> None
                        let addResponseHeader (requestData: RequestData.RequestData) name value =
                            requestData.responseHeaders.[name] <- value
                        let createSessionCookie requestData cookie =
                            addResponseHeader requestData "Set-Cookie" <|sprintf "connect.sid=%s; HttpOnly" cookie // Path={sourceUrl}; 

                        let! processed = configuration.asyncRequest {
                            Url = header.url
                            Method = header.method
                            Query = responseData.query
                            GetUrlRoot = fun () -> requestData.urlRoot.Value
                            GetText = getText requestData
                            GetCookie = getCookie requestData
                            AddResponseHeader = addResponseHeader requestData
                            CreateSessionCookie = createSessionCookie requestData
                            AsyncSendJson = Response.asyncSendJson responseData
                            AsyncSendText = Response.asyncSendText responseData
                            AsyncSendStatic = Static.asyncServeStaticUrl requestData
                            AsyncRedirect302 = Response.asyncRedirect302 requestData
                            RequestData = requestData
                        }
                    
                        if not processed then
                            do! asyncServeStatic requestData
                    else
                        let responseData = create requestData
                        do! asyncSendOption responseData
                requestSession.startReceive requestSession configuration redirectTls
        } |> Async.StartImmediate
    | None -> ()
 