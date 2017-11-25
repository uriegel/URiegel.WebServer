module UrlQueryComponents
open System
open System.Text.RegularExpressions

let urlParameterRegex = Regex (@"(?<key>[^&?]*?)=(?<value>[^&?]*)", RegexOptions.Compiled)

type Query = 
    {
        method: string
        rawParameters: Map<string,string>
    }
    member this.Query key = this.rawParameters.TryFind key

let create (url: string) = 
    let pos = url.LastIndexOf '/' + 1 
    if url.Contains "?" then
        let urlParameterString = Uri.UnescapeDataString url
        let matches = urlParameterRegex.Matches urlParameterString
        
        let unescapeSpaces (uri: string) = uri.Replace ('+', ' ')
        {
            method = url.Substring (pos, url.IndexOf ('?') - pos)
            rawParameters = matches 
                            |> Seq.cast 
                            |> Seq.map (fun (s: Match) -> (s.Groups.["key"].Value, Uri.UnescapeDataString (unescapeSpaces s.Groups.["value"].Value))) 
                            |> Map.ofSeq
        }
    else 
        {
            method = url.Substring (pos)
            rawParameters = Map.empty
        }

