using System;
using System.IO;
using System.Threading.Tasks;
using UwebServer;
using UwebServer.Routes;

var routeRequests = new Static()
{
    Method = Method.GET,
    Path = "/requests",
    FilePath = "webroot/Requests"
    // url: http://localhost:9865/requests/index.html
};

var routeWebSite = new WebSite(file => File.OpenRead(Path.Combine("webroot/Reitbeteiligung", file)))
{
    Path = "/web",
    // url: http://localhost:9865/web/index.html
};

var routeUpload = new UploadRoute("/upload", "/home/uwe/upload");

var routeBasic = new WebSite(file => File.OpenRead(Path.Combine("webroot/Reitbeteiligung", file)))
{
    Path = "/basic",
    BasicAuthentication = new()
    {
        Realm = "Reitbeteiligung",
        Name = "Test",
        Password = "pw"
    }
    // url: http://localhost:9865/basic/index.html
};

var startTime = DateTime.Now;

var routeWebSiteFirstTime = new WebSite(file => File.OpenRead(Path.Combine("webroot/Reitbeteiligung", file)), _ => startTime)
{
    Path = "/webfirst",
    // url: http://localhost:9865/webfirst/index.html
};

var routeStatic = new Static()
{
    FilePath = "webroot/Reitbeteiligung"
    // url: http://localhost:9865
};

var routeJsonRest = new JsonRest("/requests/testreq", async urlQuery => 
    {
        return new { Name = "Uwe Riegel", EMail = "uriegel@web.de" };
    });

var routeJsonService = new JsonService("/requests/testreq", async input => 
    {
        var path = input.Path;
        var inputObject = input.RequestParam.Get<JsonServiceInput>();
        return new { Name = "Uwe Riegel", EMail = "uriegel@web.de" };
    });

var routeLetsEncrypt = new LetsEncrypt();

//var routeSearch = new Redirect("/search", "https://www.google.de/");
//var routeSearch = new Redirect("/", "http://fritz.box");

var server = new Server(new Settings()
{
    Port = 9865,
    TlsPort = 4433,
    IsTlsEnabled = true,
    Routes = new Route[] 
    { 
        routeJsonRest, 
        routeJsonService, 
        routeRequests, 
        routeWebSiteFirstTime,
        routeBasic,
        routeWebSite, 
        routeUpload,
  //      routeSearch,
        routeLetsEncrypt,
        routeStatic 
    }
});

server.Start();
Console.ReadLine();
server.Stop();

record JsonServiceInput(string name, int id);

class TestRoute : Route
{
    public override Task<bool> ProcessAsync(IRequest request, IRequestHeaders requestHeaders, Response response)
    {
        Console.WriteLine($"Eingeschlagen in {Path}");
        return Task.FromResult(true);
    }
}