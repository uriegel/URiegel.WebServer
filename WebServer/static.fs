module Static
open Configuration
open Header
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

let private checkFile (url: string) (configuration: Value) = 
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
    let path = Path.Combine (configuration.WebRoot, relativePath)

    if File.Exists path then 
        path
    elif not isDirectory then
        ""
    else
        let file = Path.Combine (path, "index.html")
        if File.Exists file then file else ""

let startSendNotFound () = 
    ()

let startSendFile file = 
    ()

let startRedirectDirectory header url configuration = 
    let host = header.headers.TryFind "Host"
    let path = checkFile url configuration
    if path = "" then
        startSendNotFound ()
    elif host.IsSome && host.Value <> "" then
        let response = "<html><head>Moved permanently</head><body><h1>Moved permanently</h1>The specified resource moved permanently.</body</html>"
        let responseBytes = Encoding.UTF8.GetBytes response
        let redirectHeaders = 
            sprintf "%s 301 Moved Permanently\r\nLocation: %s%s\r\nContent-Length: %d\r\n\r\n",
            "", "", "", responseBytes.Length
        ()

// TODO: HTTPResponseString
// TODO: UrlRoot = $"http{(string)(Server.Configuration.IsTlsEnabled ? "s" : null)}://{Headers.Host}";

let serveStatic (header: Header) configuration = 
    let file = checkFile header.url configuration
    if file <> "" then  
        startSendFile file
    elif not (header.url.EndsWith "/") then
        startRedirectDirectory header (header.url + "/") configuration 
    else
        startSendNotFound ()
