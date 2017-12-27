open System.Runtime.Serialization
open Configuration
open Session

[<DataContract>]
type Command = {
    [<DataMember>]
    mutable cmd: string

    [<DataMember>]
    mutable requestId: string
}

printfn "Starting Test Server"

let asyncRequest (requestSession: RequestSession) = 
    async {
        match requestSession.query.Value.method with
        | "affe" ->
            let test = requestSession.query.Value
            let param1 = test.Query "param1" 
            let param2 = test.Query "param2"
            let param3 = test.Query "param41"

            let command = {
                cmd = "Kommando"
                requestId = "RekwestEidie"
            }
            //System.Threading.Thread.Sleep 3
            do! requestSession.asyncSendJson (command :> obj)
            return true
        | _ -> return false
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

