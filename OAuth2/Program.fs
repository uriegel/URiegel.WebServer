open System.Runtime.Serialization
open Configuration
open Session
open Request

// module ClientId
// let id = "< g u i d >"

printfn "Starting Test Server"

let getAuthUrl () = 
    sprintf "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id=%s&response_type=id_token&redirect_uri=%s&response_mode=form_post&scope=%s&state=%s&nonce=%s"
            ClientId.id "http://localhost:20000/login" "openid api%3A%2F%2F417ef561-39cf-46d7-a8a7-5b4e040c00c6%2Ftest.read" "state" "Nonsense"

let (|AccessGranted|_|) sessionId path =
    match path with 
    | Some path -> if path |> String.startsWith "token" then Some path else None
    | None -> None

let asyncRequest (requestSession: RequestSession) = 
    async {
        let request = requestSession.Query.Value
        match request.Path with
        | AccessGranted "sessionId" path -> 
            do! requestSession.AsyncSendStatic ("/" + request.Request)
            return true
        | _ -> 
            let url = 
                if request.Request = "login" then
                    "http://localhost:20000/token=23456/Reitbeteiligung"
                else
                    getAuthUrl ()
            do! Response.asyncRedirect302 url requestSession.RequestData
            return true
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
        Port=20000
        AllowOrigins = Some [| "http://localhost:8080" |]
        onNewWebSocket = onNewWebSocket
        asyncRequest = asyncRequest
        favicon = "Uwe.jpg"
}
try 
    let server = Server.create configuration 
    server.start ()
    stdin.ReadLine() |> ignore
    server.stop ()
with
    | ex -> printfn "Fehler: %O" ex



// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Caseris.Core.WebServiceTools;
// using Caseris.Http.Interfaces;

// namespace RootAuthorization
// {
// 	class AuthorizationInstance : IAuthorization
// 	{
// 		public bool Check(IAsyncSession session, IAuthorizing authorizing, string url)
// 			=> url != loginUrl ? sessions.Contains(authorizing.GetCookie("connect.sid")) : true;

// 		public async Task AuthorizeAsync(IAsyncSession session, IAuthorizing authorizing)
// 		{
// 			if (session.Headers.Method == Method.POST)
// 			{
// 				var stream = new MemoryStream();
// 				await session.ReadStreamAsync(stream);
// 				var buddy = Encoding.UTF8.GetString(stream.GetBuffer());
// 				var cookie = authorizing.GetCookie("connect.sid");

// 				sessions.Add(cookie);
// 				await authorizing.Redirect302Async("/root");
// 			}
// 			else
// 				await authorizing.RedirectToLoginAsync(loginUrl, session.Headers.Url);
// 		}

// 		string loginUrl = "/root/login.html";

// 		HashSet<string> sessions = new HashSet<string>();
// 	}
// }


		// public async Task RedirectToLoginAsync(string redirectedUrl, string sourceUrl)
		// {
		// 	if (!string.IsNullOrEmpty(Headers.Host))
		// 	{
		// 		var cookie = Guid.NewGuid();
		// 		var redirectHeaders = $"{HttpResponseString} 302 Found\r\nLocation: {UrlRoot}{redirectedUrl}\r\nSet-Cookie: connect.sid={cookie}; Path={sourceUrl}; HttpOnly\r\nContent-Length: 0\r\n\r\n";
		// 		var headerBuffer = ASCIIEncoding.ASCII.GetBytes(redirectHeaders);
		// 		await networkStream.WriteAsync(headerBuffer, 0, headerBuffer.Length);
		// 	}
		// }


		// public string GetCookie(string cookie)
		// {
		// 	var cookieString = Headers["Cookie"];
		// 	if (string.IsNullOrEmpty(cookieString))
		// 		return null;
		// 	var cookies = cookieString.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
		// 	var targetCookie = cookies.FirstOrDefault(n => n.StartsWith(cookie + "="));
		// 	return targetCookie?.Substring(targetCookie.IndexOf("=") + 1);
		// }
