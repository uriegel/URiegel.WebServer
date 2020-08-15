module ResponseData
open RequestData
open FSharpTools.UrlQueryComponents

type ResponseData =
    {
        requestData: RequestData
        response: Lazy<string>
        query: Lazy<Query>
    }

let create requestData = 
    {
        requestData = requestData
        response = Lazy<string>.Create <| fun () -> if requestData.header.http10 then "HTTP/1.0" else "HTTP/1.1"
        query = Lazy<Query>.Create <| fun () -> create requestData.header.url
    }
