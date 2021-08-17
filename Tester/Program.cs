using System;
using System.Threading.Tasks;
using UwebServer;

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

var server = new Server(new Settings()
{
    Port = 9865,
    Routes = new[] { route1, route2 }
});

server.Start();
Console.ReadLine();
server.Stop();

class TestRoute : Route
{
    public override Task ProcessAsync(Response response)
    {
        Console.WriteLine($"Eingeschlagen in {Path}");
        return Task.FromResult(0);
    }
}