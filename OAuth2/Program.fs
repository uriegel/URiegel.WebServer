open System
open Configuration
open Session

// To include clientId:
// create a source file clientid.fs (gitignore):
// module ClientId
// let id = "< g u i d >"

printfn "Starting Test Server"

let getAuthUrl originalUrl = 
    sprintf "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id=%s&response_type=id_token&redirect_uri=%s&response_mode=form_post&scope=%s&state=%s&nonce=%s"
            ClientId.id "http://localhost:20000/login" "openid api%3A%2F%2F417ef561-39cf-46d7-a8a7-5b4e040c00c6%2Ftest.read" originalUrl "Nonsense"

let (|AccessGranted | AccessDenied|) (requestSession: RequestSession) =
    match requestSession.GetCookie "connect.sid" with
    | Some cookie -> AccessGranted
    | None -> AccessDenied

let asyncRequest (requestSession: RequestSession) = 
    async {
        let request = requestSession.Query.Value
        if request.Request = "login" then
            let payload = requestSession.GetText ()
            let result = Parameters.scan payload
            let urlBase = requestSession.GetUrlRoot ()
            let originalUrl = Uri.UnescapeDataString result.["state"]

            let pad str = 
                let len = str |> String.length 
                str |> String.padRight (len + (4 - len % 4) % 4) '='


            let jwt = Uri.UnescapeDataString result.["id_token"]              
            let parts = jwt |> String.splitChar '.'

            let clientIdBase64 = 
                parts.[1]
                |> pad
                |> String.replaceChar '-' '+'
                |> String.replaceChar '_' '/'  
            let clientIdBytes = System.Convert.FromBase64String clientIdBase64
            let clientId = System.Text.Encoding.UTF8.GetString clientIdBytes               

            requestSession.CreateSessionCookie <| (Guid.NewGuid ()).ToString ()
            do! requestSession.AsyncRedirect302 <| urlBase + originalUrl
            return true
        else
            match requestSession with
            | AccessGranted -> return false
            | AccessDenied ->
                do! requestSession.AsyncRedirect302 <| getAuthUrl requestSession.Url
                return true
    }

let configuration = Configuration.create {
    Configuration.createEmpty() with 
        WebRoot = "webroot" 
        Port=20000
        AllowOrigins = Some [| "http://localhost:8080" |]
        //asyncRequest = asyncRequest
        favicon = "Uwe.jpg"
}
try 
    let server = Server.create configuration 
    server.start ()
    stdin.ReadLine() |> ignore
    server.stop ()
with
    | ex -> printfn "Fehler: %O" ex



// class AuthorizationInstance : IAuthorization
// {
// public bool Check(IAsyncSession session, IAuthorizing authorizing, string url)
// => url != loginUrl ? sessions.Contains(authorizing.GetCookie("connect.sid")) : true;

// public async Task AuthorizeAsync(IAsyncSession session, IAuthorizing authorizing)
// {
// if (session.Headers.Method == Method.POST)
// {
// var stream = new MemoryStream();
// await session.ReadStreamAsync(stream);
// var buddy = Encoding.UTF8.GetString(stream.GetBuffer());
// var cookie = authorizing.GetCookie("connect.sid");

// sessions.Add(cookie);
// await authorizing.Redirect302Async("/root");
// 
// else
// await authorizing.RedirectToLoginAsync(loginUrl, session.Headers.Url);

// string loginUrl = "/root/login.html";

// HashSet<string> sessions = new HashSet<string>();
// }
// }





