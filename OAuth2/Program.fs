open System.Runtime.Serialization
open Configuration
open Session
open Request

printfn "Starting Test Server"

let (|AccessGranted|_|) sessionId path =
    match path with 
    | Some path -> if path |> String.startsWith "token" then Some path else None
    | None -> None

let asyncRequest (requestSession: RequestSession) = 
    async {
        let request = requestSession.Query.Value
        match request.Path with
        | AccessGranted "sessionId" path -> 
            return false
        | _ -> 
            do! Response.asyncRedirect302 "https://google.de" requestSession.RequestData
            return true
    }

let onWebSocketClose _ =
    printfn "%s" "gekloßt"
    
let onNewWebSocket _ __ = 
    {
        id = ""
        onClose = onWebSocketClose
    }

let configuration = Configuration.create {
    Configuration.createEmpty() with 
        WebRoot = "webroot" 
        Port=20000
        AllowOrigins = Some [| "http://localhost:8080" |]
        onNewWebSocket = onNewWebSocket
        asyncRequest = asyncRequest
        favicon = "Uwe.jpg"
}
try 
    let server = Server.create configuration 
    server.start ()
    stdin.ReadLine() |> ignore
    server.stop ()
with
    | ex -> printfn "Fehler: %O" ex

