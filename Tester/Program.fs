open Session
open Request

type Command = {
    Cmd: string
    RequestId: string
    Count: int64
}

type Input = {
    Id: string
    Name: string
}

// TODO:
// Testprogramm: TCPSender: send Header, dann Send Json
// Testprogramm: dann SendJson > 20000 bzw. Buffersize = 1000 Json > 4000

printfn "Starting Test Server"

let request (requestSession: RequestSession) =
    async {
        let request = requestSession.Query.Value
        match requestSession.Query.Value.Request with
        | "test" ->
            let test = requestSession.Query.Value
            let param1 = test.Query "param1" 
            let param2 = test.Query "param2"
            let param3 = test.Query "param41"

            let command = {
                Cmd = "Command"
                RequestId = "RequestIDValue"
                Count= 45L
            }
            //System.Threading.Thread.Sleep 3
            do! requestSession.AsyncSendJson (command :> obj)
            return true
        | _ -> return false
    }

let asyncRequest (requestSession: RequestSession) = 
    async {
        let request = requestSession.Query.Value
        match requestSession.Query.Value.Request, request.Path with
        | "login", _ -> 
            let data = requestSession.GetText ()
            return false
        | _ -> return false
    }

let configuration = Configuration.create {
    Configuration.createEmpty() with 
        Port = 8080
        // TlsPort = 4434
        DomainName = "uriegel.de"
        //UseLetsEncrypt = true
        AllowOrigins = Some [| "http://localhost:8080" |]
        Requests = [ Proxy.proxyRequest; request ]
}

try
        // let rec getLinesBufferAt lines = 
        //     seq {
        //         match getLinesBuffer () with
        //         | Some lines -> yield! getLinesBufferAt lines
        //         | None -> yield lines
        //     }

        // getLinesBufferAt  |> Seq.concat

        
    //let seq2 = getLines ()

    // printfn "\nThe"
    // let ret = seq2 |> Seq.toArray



//    printfn "\nThe sequence fib contains Fibonacci numbers. %d" ret.Length
    // for x in ret do printf "%O " x
    // printfn ""

    let server = Server.create configuration 
    server.start ()
    stdin.ReadLine() |> ignore
    server.stop ()
with
    | ex -> printfn "Fehler: %O" ex

