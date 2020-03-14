module Request
open Header
open Response
open ResponseData
open Static
open Session
open System.IO
open System.Text

let getJson<'T> (requestSession: RequestSession) = 
    let requestDataValue = requestSession.RequestData :?> RequestData.RequestData
    let buffer = requestDataValue.buffer
    use memStm = new MemoryStream ( buffer.buffer, buffer.currentIndex, buffer.read - buffer.currentIndex)
    Json.deserializeStream<'T> memStm

let private asyncValidateLetsEncrypt (responseData: ResponseData) = async {
    let token = File.ReadAllText("/etc/letsencrypt-uweb/token")
    printfn "Validating LetsEncrypt token : %s" token
    do! asyncSendText responseData token
}   

let private asyncTlsRedirect (requestData: RequestData.RequestData) = async {
    let response = "<html><head>Moved permanently</head><body><h1>Moved permanently</h1>The specified resource moved permanently.</body</html>"
    let responseBytes = Encoding.UTF8.GetBytes response
    let redirectHeaders = 
        sprintf "HTTP/1.1 301 Moved Permanently\r\nLocation: %s%s\r\nContent-Length: %d\r\n\r\n"
            ("https://" + requestData.configuration.DomainName) requestData.header.url responseBytes.Length
    let headerBytes = Encoding.UTF8.GetBytes redirectHeaders 

    do! requestData.session.networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
    do! requestData.session.networkStream.AsyncWrite (responseBytes, 0, responseBytes.Length)
}

let private request (responseData: ResponseData) (request :RequestSession->Async<bool>) = 
    let getText () () = 
        let buffer = responseData.requestData.buffer
        System.Text.Encoding.UTF8.GetString (buffer.buffer, buffer.currentIndex, buffer.read - buffer.currentIndex)

    let getCookie () cookie = 
        match responseData.requestData.header.Header "Cookie" with
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

    request {
        Url = responseData.requestData.header.url
        Method = responseData.requestData.header.method
        Query = responseData.query
        GetUrlRoot = fun () -> responseData.requestData.urlRoot.Value
        GetText = getText ()
        GetCookie = getCookie ()
        AddResponseHeader = addResponseHeader responseData.requestData
        CreateSessionCookie = createSessionCookie responseData.requestData
        AsyncSendJson = Response.asyncSendJson responseData
        AsyncSendText = Response.asyncSendText responseData
        AsyncRedirect302 = Response.asyncRedirect302 responseData.requestData
        RequestData = responseData.requestData
    }

let private asyncProbeRequests (responseData: ResponseData) (requests: (RequestSession->Async<bool>) list) = async {
    let rec asyncProbeRequest (requests: (RequestSession->Async<bool>) list) = async {
        let! rest, success = async {
            match requests with
            | head :: tail -> 
                let! res = request responseData head 
                return tail, res
            | [] -> return [], false
        }
        match rest, success with
        | _, true -> return true
        | [], _ -> return false
        | _ -> return! asyncProbeRequest rest
    }
    return! asyncProbeRequest requests
}

let startRequesting headerResult configuration requestSession buffer redirectTls =
    match initialize headerResult with
    | Some header ->
        let requestData = RequestData.create configuration header requestSession buffer
        let responseData = create requestData
        
        async {
            try
                if redirectTls then
                    if header.url |> String.startsWith "/.well-known/acme-challenge" then
                        do! asyncValidateLetsEncrypt responseData
                    else
                        do! asyncTlsRedirect requestData
                else
                    if header.method <> Method.Options then
                        let! processed = asyncProbeRequests responseData configuration.Requests
                        if not processed then
                            do! asyncSendNotFound responseData                            
                    else
                        do! asyncSendOption responseData
                requestSession.startReceive requestSession configuration redirectTls
            with 
            | ex -> printfn "Exception in requesting: %O" ex
        } |> Async.StartImmediate
    | None -> ()
 
let private serveStatic directory webPath (requestSession: RequestSession) = async {
    match requestSession.Url |> String.startsWith webPath with
    | true -> 
        do! asyncServeStatic directory (requestSession.RequestData :?> RequestData.RequestData)
        return true
    | _ -> return false
}

let private asyncServeFavicon iconPath (requestSession: RequestSession) = async {
    match requestSession.Url with
    | "/favicon.ico" ->
        let requestData = requestSession.RequestData :?> RequestData.RequestData
        let responseData = create requestData
        if File.Exists iconPath then
            do! asyncSendFile iconPath responseData
        else
            do! asyncSendNotFound responseData
        return true
    | _ -> return false
}

let useStatic directory webPath = serveStatic directory webPath

let useFavicon iconPath = asyncServeFavicon iconPath

