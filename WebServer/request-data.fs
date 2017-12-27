module RequestData
open Configuration
open Header
open RequestTypes

type RequestData = {
    configuration: Value
    header: Header
    session: RequestSession
    urlRoot: Lazy<string>
}

let create configuration header session = 
    {
        configuration = configuration
        header = header
        session = session
        urlRoot = Lazy<string>.Create <| fun () -> sprintf "http%s://%s" (if configuration.IsTlsEnabled then "s" else "") header.host.Value
    }

