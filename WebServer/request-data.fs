module RequestData
open Configuration
open Header
open RequestTypes
open System.Collections.Generic

type RequestData = {
    configuration: Value
    header: Header
    buffer: Buffer
    session: RequestSession
    urlRoot: Lazy<string>
    responseHeaders: Dictionary<string, string> 
}

let create configuration header session buffer = 
    {
        configuration = configuration
        header = header
        buffer = buffer
        session = session
        urlRoot = Lazy<string>.Create <| fun () -> sprintf "http%s://%s" (if configuration.IsTlsEnabled then "s" else "") header.host.Value
        responseHeaders = Dictionary<string, string>() 
    }

