module Parameters

let scan payload = 
    payload 
    |> String.trimChars [| '\r' |]
    |> String.trimChars [| '\n' |]
    |> String.splitChar '&'
    |> Seq.map (fun n -> n |> String.splitChar '=')  
    |> Seq.fold (fun (mp: Map<string, string>) n -> mp.Add (n.[0], n.[1])) Map.empty<string, string>
