module WebSocket
open Header

let upgrade (header: Header) = 
    let secKey = header.Header "Sec-WebSocket-Key"
    printfn "%s" secKey.Value
    ()