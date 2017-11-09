module ResponseHeaders
open System

let initialize (initialHeaders: Map<string, string>) contentType (contentLength: int) lastModified noCache =
    let mutable headers = initialHeaders
    if contentType <> "" then headers <- headers.Add("Content-Type", contentType)
    if lastModified <> "" then headers <- headers.Add("Last-Modified", lastModified)
    headers <- headers.Add("Content-Length", contentLength.ToString ())
    if noCache then
        headers <- headers.Add("Cache-Control", "no-cache,no-store")
        headers <- headers.Add("Expires", (DateTime.Now.Subtract(TimeSpan(1, 0, 0))).ToUniversalTime().ToString "r")
    headers