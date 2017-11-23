open System.Runtime.Serialization
open Configuration

[<DataContract>]
type Command = {
    [<DataMember>]
    mutable cmd: string

    [<DataMember>]
    mutable requestId: string
}

printfn "Starting Test Server"

let request (url: string) responseData = 
    async {
        if url.StartsWith("/affe") then 
            let command = {
                cmd = "Kommando"
                requestId = "RekwestEidie"
            }
            do! Response.asyncSendJson responseData command
            return true
        else 
            return false
}

let configuration = Configuration.create {
        Configuration.createEmpty() with 
            Port = 20000; 
            //WebRoot = "/home/uwe/Projekte/Node/WebServerElectron/web/" 
            WebRoot = "D:\Projekte\WebServerSharp\web" 
    }
    
try
    let server = Server.create configuration
    server.registerRequests request
    server.start ()
    stdin.ReadLine() |> ignore
    server.stop ()
with
    | ex -> printfn "Fehler: %O" ex

