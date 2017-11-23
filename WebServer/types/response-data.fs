module ResponseData
open RequestData

type ResponseData =
    {
        requestData: RequestData
        response: Lazy<string>
    }

let create requestData = 
    {
        requestData = requestData
        response = Lazy<string>.Create <| fun () -> if requestData.header.http10 then "HTTP/1.0" else "HTTP/1.1"
    }
