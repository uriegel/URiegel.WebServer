module Header
open RequestTypes
open System

type Method =
    Get = 0 
    | Post = 1
    | Head = 2


type Header = {
    method: Method
    url: string
    http: string
    http10: bool
}

let initialize headerResult = 
    let headerParts = headerResult.header.Split(Array.create 1 "\r\n", StringSplitOptions.RemoveEmptyEntries)

    let method = 
        if headerParts.[0].StartsWith("GET") then
            Method.Get
        elif headerParts.[0].StartsWith("POST") then
            Method.Post
        elif headerParts.[0].StartsWith("HEAD") then
            Method.Head
        else
            failwithf "Unknown HTTP method %s" <| headerParts.[0]           

    let start = headerParts.[0].IndexOf(' ') + 1
    let url = headerParts.[0].Substring(start, headerParts.[0].IndexOf(" HTTP") - start)

    let start = headerParts.[0].IndexOf(' ', start) + 1
    let http = headerParts.[0].Substring(start)
    let http10 = String.Compare(http, "http/1.0", true) = 0            
    {
        method = method
        url = url
        http = http
        http10 = http10
    }