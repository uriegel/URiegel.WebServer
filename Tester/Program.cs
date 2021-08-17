using System;
using UwebServer;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var route1 = new Route
            {
                Method = Method.GET,
                Path = "/test",
                Process = () => Console.WriteLine("Eingeschlagen")
            };
            var route2 = new Route
            {
                Method = Method.GET,
                Path = "/path",
                Process = () => Console.WriteLine("Eingeschlagen in path")
            };
            var route3 = new Route
            {
                Method = Method.GET,
                Process = () => Console.WriteLine("Eingeschlagen im Rest")
            };

            var server = new Server(new Settings()
            {
                Port = 9865,
                Routes = new[] { route1, route2 }
            });

            server.Start();
            Console.ReadLine();
            server.Stop();
        }
    }
}
