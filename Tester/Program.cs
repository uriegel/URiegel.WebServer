﻿using System;
using System.Threading.Tasks;
using UwebServer;
using UwebServer.Routes;

// TODO: Send FileStream (from resource): Static with callback
// TODO: FirstTimeModified behavior

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
var routeJsonService = new JsonService("/requests/testreq", async input => 
    {
        var path = input.Path;
        var inputObject = input.RequestParam.Get<JsonServiceInput>();
        return new { Name = "Uwe Riegel", EMail = "uriegel@web.de" };
    });

var server = new Server(new Settings()
{
    Port = 9865,
    Routes = new Route[] { routeJsonRest, routeJsonService, routeRequests, routeStatic }
});

server.Start();
Console.ReadLine();
server.Stop();

record JsonServiceInput(string name, int id);

class TestRoute : Route
{
    public override Task ProcessAsync(IRequest request, IRequestHeaders requestHeaders, Response response)
    {
        Console.WriteLine($"Eingeschlagen in {Path}");
        return Task.FromResult(0);
    }
}