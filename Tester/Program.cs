using System;
using System.Threading.Tasks;
using UwebServer;
using UwebServer.Routes;

// TODO: JsonPost: JsonConvert.DeserializeObject<T>
// TODO: Send FileStream (from resource): Static with callback

var routeRequests = new Static()
{
    Method = Method.GET,
    Path = "/requests",
    FilePath = "webroot/Requests"
    // url: http://localhost:9865/requests/index.html
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

var server = new Server(new Settings()
{
    Port = 9865,
    Routes = new Route[] { routeJsonRest, routeRequests, routeStatic }
});

server.Start();
Console.ReadLine();
server.Stop();

class TestRoute : Route
{
    public override Task ProcessAsync(IRequestHeaders requestHeaders, Response response)
    {
        Console.WriteLine($"Eingeschlagen in {Path}");
        return Task.FromResult(0);
    }
}