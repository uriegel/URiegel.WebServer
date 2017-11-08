module Static
open RequestData
open ResponseData
open Response
open System
open System.IO
open System.Text

type QueryValues = {
    query: string
    url: string
}

type StaticInfo = {
    localFile: string
    redirUrl: string
    isFile: bool
}

let private checkFile (url: string) (requestData: RequestData) = 
    let r = url.IndexOf '#'

    let rawUrl = 
        if r <> -1 then 
            url.Substring (0, r)
        else
            url

    let qm = rawUrl.IndexOf ('?')
    let url = if qm <> -1 then rawUrl.Substring (0, qm) else rawUrl

    let isDirectory = url.EndsWith "/"

    let unescapedUrl = Uri.UnescapeDataString url
    
    let relativePath = 
        let relativePath = if Path.DirectorySeparatorChar <> '/' then unescapedUrl.Replace ('/', Path.DirectorySeparatorChar) else unescapedUrl
        relativePath.Substring 1
    let path = Path.Combine (requestData.configuration.WebRoot, relativePath)

    if File.Exists path then 
        path
    elif not isDirectory then
        ""
    else
        let file = Path.Combine (path, "index.html")
        if File.Exists file then file else ""

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

let asyncSendFile file responseData = async {
    do! asyncSendNotFound responseData
}

let asyncRedirectDirectory url (responseData: ResponseData) = async {
    let path = checkFile url responseData.requestData
    if path = "" then
        do! asyncSendNotFound responseData
    elif responseData.requestData.header.host.Value <> "" then
        let response = "<html><head>Moved permanently</head><body><h1>Moved permanently</h1>The specified resource moved permanently.</body</html>"
        let responseBytes = Encoding.UTF8.GetBytes response
        let redirectHeaders = 
            sprintf "%s 301 Moved Permanently\r\nLocation: %s%s\r\nContent-Length: %d\r\n\r\n"
                responseData.response.Value responseData.requestData.urlRoot.Value url responseBytes.Length
        let headerBytes = Encoding.UTF8.GetBytes redirectHeaders 

        do! responseData.requestData.session.networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
        do! responseData.requestData.session.networkStream.AsyncWrite (responseBytes, 0, responseBytes.Length)
}

let serveStatic (requestData: RequestData) = async {
    let responseData = create requestData
    let file = checkFile requestData.header.url requestData
    if file <> "" then  
        do! asyncSendFile file responseData
    elif not (requestData.header.url.EndsWith "/") then
        do! asyncRedirectDirectory (requestData.header.url + "/") responseData 
    else
        do! asyncSendNotFound responseData
}