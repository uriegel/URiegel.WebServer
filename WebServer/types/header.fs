module Header
open RequestTypes
open System

type Method =
    Get = 0 
    | Post = 1
    | Head = 2

type ContentEncoding = 
    None = 0
    | Deflate = 1
    | GZip = 2

type Header = 
    {
        method: Method
        url: string
        http: string
        http10: bool
        rawHeaders: Map<string,string>
        host: Lazy<string>
        contentEncoding: Lazy<ContentEncoding>
    }
    member this.Header key = 
        let value = this.rawHeaders.TryFind key
        match value with
        | Some x -> x
        | None -> ""

let initialize headerResult = 
    let headerParts = headerResult.header.Split ([|"\r\n"|], StringSplitOptions.RemoveEmptyEntries)

    let method = 
        if headerParts.[0].StartsWith("GET") then
            Method.Get
        elif headerParts.[0].StartsWith("POST") then
            Method.Post
        elif headerParts.[0].StartsWith("HEAD") then
            Method.Head
        else
            failwithf "Unknown HTTP method %s" <| headerParts.[0]           

    let start = headerParts.[0].IndexOf (' ') + 1
    let url = headerParts.[0].Substring (start, headerParts.[0].IndexOf(" HTTP") - start)

    let start = headerParts.[0].IndexOf (' ', start) + 1
    let http = headerParts.[0].Substring (start)
    let http10 = String.Compare (http, "http/1.0", true) = 0            

    let headers = 
        headerParts 
        |> Seq.skip 1 
        |> Seq.map (fun s -> (s.Substring(0, s.IndexOf(": ")), s.Substring(s.IndexOf(": ") + 2)))
        |> Map.ofSeq
    {
        method = method
        url = url
        http = http
        http10 = http10
        rawHeaders = headers
        host = Lazy<string>.Create <| fun () -> 
            let value = headers.TryFind "Host"
            match value with
                | Some x -> x
                | None -> ""
        contentEncoding = Lazy<ContentEncoding>.Create <| fun () ->
            let value = headers.TryFind "Accept-Encoding"
            match value with
                | Some acceptEncoding when acceptEncoding.Contains "deflate" -> ContentEncoding.Deflate
                | Some acceptEncoding when acceptEncoding.Contains "gzip" -> ContentEncoding.GZip
                | _ -> ContentEncoding.None
    }

    