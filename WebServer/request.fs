module Request
open RequestTypes
let request (headerResult: HeaderResult) =
    printfn "%s" headerResult.header
    