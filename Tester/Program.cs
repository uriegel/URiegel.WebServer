using System;
using UwebServer;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server(new Settings());

            server.Start();
            Console.ReadLine();
            server.Stop();
        }
    }
}
