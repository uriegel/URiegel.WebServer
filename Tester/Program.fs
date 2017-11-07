open Configuration
open Server

printfn "Starting Test Server"
let configuration = Configuration.create {
        Configuration.createEmpty() with 
            Port = 20000; 
            //WebRoot = "/home/uwe/Projekte/Node/WebServerElectron/web" 
            WebRoot = @"D:\Projekte\WebServerSharp\web"
    }

type test =
    {
        x: int
        y: Lazy<int>
    }
    member this.Sum = this.x + this.y.Value

let affe = {
    x = 9
    y = Lazy<int>.Create(fun () -> 
    printfn "Lege an"
    34)
}

let tes = affe.Sum

try
    let server = create configuration
    server.start ()
    stdin.ReadLine() |> ignore
    server.stop ()
with
    | ex -> printfn "Fehler: %O" ex

