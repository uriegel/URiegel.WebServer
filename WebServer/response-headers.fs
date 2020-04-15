module ResponseHeaders
open System
open System.Runtime.InteropServices

let private server = sprintf "UwebServer %s" <| Environment.OSVersion.ToString ()


let initialize (initialHeaders: Map<string, string>) contentType (contentLength: int option) =
    let mutable headers = initialHeaders
    headers <- headers.Add("Server", server)
    headers <- headers.Add("Date", (DateTime.Now.ToUniversalTime ()).ToString "R")
    if contentType <> "" then headers <- headers.Add("Content-Type", contentType)
    match contentLength with
    | Some length -> headers <- headers.Add ("Content-Length", length.ToString ())
    | None -> ()

    if not (headers.ContainsKey ("Connection")) && not (headers.ContainsKey ("Content-Length")) then
        headers <- headers.Add ("Connection", "close")

    headers