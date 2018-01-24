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

// TODO:
// Testprogramm: TCPSender: send Header, dann Send Json
// Testprogramm: dann SendJson > 20000 bzw. Buffersize = 1000 Json > 4000

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

[<DataContract>]
type Input = {
    [<DataMember>]
    mutable directory: string

    [<DataMember>]
    mutable id: string

    [<DataMember>]
    mutable requestNumber: int

    [<DataMember>]
    mutable affe: int
}
let asyncRequestTestJson (requestSession: RequestSession) = 
    async {
        let method = requestSession.url.Substring(requestSession.url.LastIndexOf('/') + 1) 
        match method with
        | "getItems" -> 
            let jason = requestSession.asyncGetJson typedefof<Input> :?> Input
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
        
        
        
        //WebRoot = "webroot" 
        WebRoot = "C:\Users\urieg\Documents\Projekte\Commander\WebApp"
        Port = 20000
        
        
        
        onNewWebSocket = onNewWebSocket
        asyncRequest = asyncRequestTestJson
        favicon = "Uwe.jpg"
}
    
try
   let server = Server.create configuration 
   server.start ()
   stdin.ReadLine() |> ignore
   server.stop ()
with
   | ex -> printfn "Fehler: %O" ex

