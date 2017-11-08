module Static
open RequestData
open ResponseData
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

let startSendNotFound () = 
    ()

let startSendFile file = 
    ()

let startRedirectDirectory url (responseData: ResponseData) = 
    let path = checkFile url responseData.requestData
    if path = "" then
        startSendNotFound ()
    elif responseData.requestData.header.host.Value <> "" then
        let response = "<html><head>Moved permanently</head><body><h1>Moved permanently</h1>The specified resource moved permanently.</body</html>"
        let responseBytes = Encoding.UTF8.GetBytes response
        let redirectHeaders = 
            sprintf "%s 301 Moved Permanently\r\nLocation: %s%s\r\nContent-Length: %d\r\n\r\n"
                responseData.response.Value responseData.requestData.header.host.Value url responseBytes.Length
        let affe = redirectHeaders
        ()

let serveStatic (requestData: RequestData) = 

    let resposenData = create requestData
    let file = checkFile requestData.header.url requestData
    if file <> "" then  
        startSendFile file
    elif not (requestData.header.url.EndsWith "/") then
        startRedirectDirectory (requestData.header.url + "/") resposenData 
    else
        startSendNotFound ()
