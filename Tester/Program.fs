open Configuration

printfn "Starting Test Server"

let request (url: string) responseData = 
    async {
        if url.StartsWith("/affe") then 
            printfn "Die Urle: %s" url

            do! Response.asyncSendError responseData @"<title>CAESAR</title>
<Style> 
html {
    font-family: sans-serif;
}
h1 {
    font-weight: 100;
}
</Style>"           "<h1>Datei nicht gefunden, weil nicht da</h1><p>Die angegebene Resource konnte auf dem Server nicht gefunden werden, da sie nicht gesucht wurde.</p>" 404 "Not Found" 

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

