open Configuration
open Server

printfn "Starting Test Server"
let configuration = Configuration.create {
        Configuration.createEmpty() with 
            Port = 20000; 
            WebRoot = "/home/uwe/Projekte/Commander/" 
            //WebRoot = @"C:\sourcen\Main\projects\Test\WebRTC"
    }

try
    let server = create configuration
    server.start ()
    stdin.ReadLine() |> ignore
    server.stop ()
with
    | ex -> printfn "Fehler: %O" ex

