module Static
open MimeTypes
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

let private checkFile directory url = 
    let rawUrl = 
        match url |> String.indexOfChar '#' with
        | Some pos -> url |> String.substring2 0 pos
        | None -> url

    let url = 
        match rawUrl |> String.indexOfChar '?' with
        | Some pos -> rawUrl |> String.substring2 0 pos
        | None -> rawUrl

    let isDirectory = url.EndsWith "/"

    let unescapedUrl = Uri.UnescapeDataString url
    
    let relativePath = 
        let relativePath = if Path.DirectorySeparatorChar <> '/' then unescapedUrl.Replace ('/', Path.DirectorySeparatorChar) else unescapedUrl
        relativePath.Substring 1
    let path = Path.Combine (directory, relativePath)
    let path = 
        if Path.IsPathRooted path then
            path
        else
            Path.Combine(Directory.GetCurrentDirectory(), path)

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

let asyncSendRange file responseData = async {
    ()
}

let asyncSend304 responseData = async {
    let headerString = sprintf "%s 304 Not Modified\r\n\r\n" responseData.response.Value
    let headerBytes = ASCIIEncoding.ASCII.GetBytes headerString
    do! responseData.requestData.session.networkStream.AsyncWrite (headerBytes, 0, headerBytes.Length)
}

let asyncInternalSendFile file responseData = async {
    let fi = FileInfo file

    //let noCache = Server.Configuration.NoCacheFiles.Contains(file.ToLower());
    let noCache = false
    if not noCache then
        // TODO: AppCache
        ()

    let isModifiedSince = 
        match responseData.requestData.header.Header "If-Modified-Since" with
        | None -> None
        | Some a when a = "" -> None
        | Some v -> 
            let pos = v.IndexOf ";"
            Some (if pos <> -1 then v.Substring (0, pos) else v)
        
    let modified =  
        match isModifiedSince with
        | Some v ->
            let ifModifiedSince = Convert.ToDateTime v
            let fileTime = fi.LastWriteTime.AddTicks -(fi.LastWriteTime.Ticks % TimeSpan.FromSeconds(1.0).Ticks)
            let diff = fileTime - ifModifiedSince
            diff > TimeSpan.FromMilliseconds 0.0
        | None -> true

    if modified then
        let contentType = 
            match fi.Extension with
            | ".html" 
            | ".htm" -> "text/html; charset=UTF-8"
            | ".css" -> "text/css; charset=UTF-8"
            | ".js" -> "application/javascript; charset=UTF-8"
            | ".appcache" -> "text/cache-manifest"
            | _ ->  getMimeType fi.Extension
       
        let dateTime = fi.LastWriteTime
        let lastModified = dateTime.ToUniversalTime().ToString "r"

        use stream = File.OpenRead file
        do! asyncSendStream responseData stream contentType lastModified
    else
        do! asyncSend304 responseData
}

let asyncSendFile (file: string) responseData = async {
    if file.EndsWith (".mp4", StringComparison.InvariantCultureIgnoreCase)
        || file.EndsWith (".mkv", StringComparison.InvariantCultureIgnoreCase)
        || file.EndsWith (".mp3", StringComparison.InvariantCultureIgnoreCase)
        || file.EndsWith (".wav", StringComparison.InvariantCultureIgnoreCase) then
        do! asyncSendRange file responseData
    else
        do! asyncInternalSendFile file responseData
}

let asyncRedirectDirectory directory url responseData = async {
    let path = checkFile directory url
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

let rec asyncServeStaticUrl directory requestData url = async {
    let responseData = create requestData
    let file = checkFile directory url
    if file <> "" then  
        do! asyncSendFile file responseData
    elif not (url.EndsWith "/") then
        do! asyncRedirectDirectory directory (url + "/") responseData
    else
        do! asyncSendNotFound responseData
}

let asyncServeStatic directory requestData = 
    asyncServeStaticUrl directory requestData requestData.header.url

