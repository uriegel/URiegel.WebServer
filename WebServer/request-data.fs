module RequestData
open Configuration
open RequestTypes
open System.Collections.Generic
open Session

type RequestData = {
    configuration: Value
    header: Header
    buffer: Buffer
    session:  RawRequestSession
    urlRoot: Lazy<string>
    responseHeaders: Dictionary<string, string> 
}

let create configuration header session buffer = 
    {
        configuration = configuration
        header = header
        buffer = buffer
        session = session
        urlRoot = Lazy<string>.Create <| fun () -> sprintf "http%s://%s" (if configuration.UseLetsEncrypt then "s" else "") header.host.Value
        responseHeaders = Dictionary<string, string>() 
    }

