module ResponseHeaders
open System
open System.Runtime.InteropServices

let private server = sprintf "UwebServer %s" <| Environment.OSVersion.ToString ()


let initialize (initialHeaders: Map<string, string>) contentType contentLength lastModified noCache =
    let mutable headers = initialHeaders
    headers <- headers.Add("Server", server)
    headers <- headers.Add("Date", (DateTime.Now.ToUniversalTime ()).ToString "R")
    if contentType <> "" then headers <- headers.Add("Content-Type", contentType)
    if lastModified <> "" then headers <- headers.Add("Last-Modified", lastModified)
    headers <- headers.Add("Content-Length", contentLength.ToString ())
    if noCache then
        headers <- headers.Add("Cache-Control", "no-cache,no-store")
        headers <- headers.Add("Expires", (DateTime.Now.Subtract(TimeSpan(1, 0, 0))).ToUniversalTime().ToString "r")
    headers