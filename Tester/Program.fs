open System.Runtime.Serialization
open Configuration
open Session
open System.IO

[<DataContract>]
type Command = {
    [<DataMember>]
    mutable cmd: string

    [<DataMember>]
    mutable requestId: string

    [<DataMember>]
    mutable count: int64
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
                count= 45L
            }
            //System.Threading.Thread.Sleep 3
            do! requestSession.asyncSendJson (command :> obj)
            return true
        | _ -> return false
    }


let teste () = 
//     use fs = File.OpenRead("/home/uwe/server1.log")
//     let buffer = Array.zeroCreate 20000000
    
//     let getLines () =
//         let read = fs.Read (buffer, 0, buffer.Length)

//         let getLinesFromBuffer (buffer: byte array) =
//             Seq.unfold (fun (a, b) -> compare buffer 0L read '\n')
//             | Seq.takewhile (fun i -> i < 9)
//             let lineIndex = 
//             4

    4L


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
            //let jason = requestSession.asyncGetJson typedefof<Input> :?> Input
            //requestSession.asyncSendJson "Affe"

            let count = teste ()

            let command = {
                cmd = "Kommando"
                requestId = "RekwestEidie"
                count = count
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
        //WebRoot = "C:\Users\urieg\Documents\Projekte\Commander\WebApp"
        Port = 20000
        
        
        
        onNewWebSocket = onNewWebSocket
        asyncRequest = asyncRequestTestJson
        favicon = "Uwe.jpg"
}

type Line = {
    pos: int64
    length: int64
}


try


    use fs = File.OpenRead("/home/uwe/server.log")
    let buffer = Array.zeroCreate 200000
    
    let getLines () =
        let getLinesBuffer () =
            let fileOffset = fs.Position
            if fileOffset < fs.Length then

                let read = fs.Read (buffer, 0, buffer.Length)

                let rec findChr (buffer: byte array) index maxLength searchChr = 
                    match index < maxLength with
                    | true when buffer.[index] = searchChr -> Some index
                    | true -> findChr buffer (index + 1) maxLength searchChr
                    | false -> None 

                Some (0L  // Initial state
                    |> Seq.unfold (fun state ->
                        match findChr buffer (int state) read (byte '\n') with
                        | Some pos -> Some({ pos = state + fileOffset; length = (int64 pos - state - 1L) }, int64 (pos + 1))
                        | None -> None
                    )
                )
            else
                None

        let ret = 
            0 |> Seq.unfold (fun state ->
                match getLinesBuffer () with
                | Some lines -> Some (lines, 0)
                | None -> None) |> Seq.concat
        ret
        
        // let rec getLinesBufferAt lines = 
        //     seq {
        //         match getLinesBuffer () with
        //         | Some lines -> yield! getLinesBufferAt lines
        //         | None -> yield lines
        //     }

        // getLinesBufferAt  |> Seq.concat

        
    let seq2 = getLines ()

    printfn "\nThe"
    let ret = seq2 |> Seq.toArray



    printfn "\nThe sequence fib contains Fibonacci numbers. %d" ret.Length
    // for x in ret do printf "%O " x
    // printfn ""

    let server = Server.create configuration 
    server.start ()
    stdin.ReadLine() |> ignore
    server.stop ()
with
    | ex -> printfn "Fehler: %O" ex

