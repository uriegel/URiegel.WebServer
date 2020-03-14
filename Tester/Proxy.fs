module Proxy
open Session
open System.Net

let (|Int|_|) (str:string) =
    match System.Int32.TryParse str with
    | true,int -> Some int
    | _ -> None

let proxyRequest (requestSession: RequestSession) =
    async {
        let urlRoot = requestSession.GetUrlRoot ()
        match urlRoot |> String.contains "127.0.0.40" with
        | true -> 

            let url = "http://192.168.178.1"

            let request = requestSession.Query.Value
            let webRequest = WebRequest.Create url :?> HttpWebRequest
            webRequest.Method <- string requestSession.Method |> String.toUpperInvariant
            //var body = Headers.Method == Method.POST ? Get() : null;
            for h in requestSession.Header.rawHeaders do
                match h.Key |> String.toLowerInvariant with
                | "accept" -> webRequest.Accept <- h.Value 
                | "connection" when h.Value <> "Keep-Alive" -> 
                    webRequest.KeepAlive <- false
                | "if-modified-since" -> 
                    let dts =  
                        match h.Value |> String.indexOfChar ';' with
                        | Some pos -> h.Value |> String.substring2 0 pos
                        | None -> h.Value
                    let dt = System.DateTime.Parse (dts.Trim())
                    webRequest.IfModifiedSince <- dt
                | "content-length" -> 
                    match h.Value with
                    | Int value -> webRequest.ContentLength <- int64 value
                    | _ -> printf "Could not set Content-Length"
                | "content-type" -> webRequest.ContentType <- h.Value
                | "host" -> ()
                | "user-agent" -> webRequest.UserAgent <- h.Value
                | "referer" -> webRequest.Referer <- h.Value
                | _ -> 
                    try
                        webRequest.Headers.Add(h.Key + ": " + h.Value)
                    with
                    | e -> printf "Could not redirect: %O" e

            // if (addXForwardedUri)
            //     webRequest.Headers.Add($"X-Forwarded-URI: {CreateXForwarded()}");                    

                // HttpWebResponse response = null;
                // try
                // {
                //     webRequest.CertificateValidator(e =>
                //     {
                //         Logger.Current.Warning($"{Id} {e.Message}");
                //         e.ChainErrorDescriptions?.Perform(n =>
                //         {
                //             Logger.Current.Warning($"{Id} {n}");
                //             return true;
                //         });
                //         return false;
                //     });
                //     response = (HttpWebResponse)await webRequest.GetResponseAsync();
                // }
                // catch (WebException we)
                // {
                //     if (we.Response == null)
                //         throw we;
                //     response = (HttpWebResponse)we.Response;
                // }
                let! response = webRequest.GetResponseAsync () |> Async.AwaitTask 
                let httpResponse = response :?> HttpWebResponse

                let strom = response.GetResponseStream ()
                let responseHeaders = 
                    response.Headers.AllKeys 
                    |> Array.map (fun key -> sprintf "%s: %s" key response.Headers.[key])

                ()

                

//     responseHeaders = responseHeaders.Where(n => !n.StartsWith("allow:", StringComparison.InvariantCultureIgnoreCase)
//         && !n.StartsWith("connection:", StringComparison.InvariantCultureIgnoreCase));
//     var headerString = string.Join("\r\n", responseHeaders) + "\r\n\r\n";
//     var html = $"{HttpResponseString} {(int)response.StatusCode} {response.StatusDescription}\r\n" + headerString;
//     var htmlBytes = Encoding.UTF8.GetBytes(html);
//     await WriteAsync(htmlBytes, 0, htmlBytes.Length);
//     await WriteStreamAsync(strom);
//     return true;
// }
// catch (Exception e)
// {
//     Logger.Current.LowTrace(() => $"An error has occurred while redirecting: {e}");
//     try
//     {
//         await SendExceptionAsync(e);
//     }
//     catch { }
// 	return false;
// }


            return true
        | false -> return false
    }


//         case "range":
//             try
//             {
//                 var sizes = h.Value.Value.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(1)
//                     .Select(n => long.Parse(n)).ToArray();
//                 if (sizes.Length > 1)
//                     webRequest.AddRange(sizes[0], sizes[1]);
//             }
//             catch (Exception e)
//             {
//                 Logger.Current.Warning($"{Id} Error occurred in range: {e}");
//             }
//             break;
