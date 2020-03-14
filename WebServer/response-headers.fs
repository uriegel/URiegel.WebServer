module ResponseHeaders
open System
open System.Runtime.InteropServices

let private server = sprintf "UwebServer %s" <| Environment.OSVersion.ToString ()


let initialize (initialHeaders: Map<string, string>) contentType (contentLength: int option) lastModified noCache =
    let mutable headers = initialHeaders
    headers <- headers.Add("Server", server)
    headers <- headers.Add("Date", (DateTime.Now.ToUniversalTime ()).ToString "R")
    if contentType <> "" then headers <- headers.Add("Content-Type", contentType)
    if lastModified <> "" then headers <- headers.Add("Last-Modified", lastModified)
    match contentLength with
    | Some length -> headers <- headers.Add ("Content-Length", length.ToString ())
    | None -> ()
    if noCache then
        headers <- headers.Add("Cache-Control", "no-cache,no-store")
        headers <- headers.Add("Expires", (DateTime.Now.Subtract(TimeSpan(1, 0, 0))).ToUniversalTime().ToString "r")

    if not (headers.ContainsKey ("Content-Length")) then
        headers <- headers.Add ("Connection", "close")

    headers