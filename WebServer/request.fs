module Request
open Header

let request headerResult =
    let header = initialize headerResult
    printfn "%O" header.method
    printfn "%s" header.http
    printfn "%s" header.url
    printfn "%O" header.http10
    printfn "%O" header.headers

    printfn "%s" header.headers.["Connection"]
    