using System;
using System.Threading.Tasks;
using UwebServer;
using UwebServer.Routes;

var route1 = new TestRoute()
{
    Method = Method.GET,
    Path = "/test"
};
var route2 = new TestRoute()
{
    Method = Method.GET,
    Path = "/path",
};
var route3 = new TestRoute()
{
    Method = Method.GET,
};

var routeStatic = new Static()
{
    Path = "/web",
    FilePath = "webroot/Reitbeteiligung"
};

var server = new Server(new Settings()
{
    Port = 9865,
    Routes = new Route[] { route1, route2, routeStatic }
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