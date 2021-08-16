using System;
using UwebServer;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server(new Settings()
            {
                Port = 9865
            });

            server.Start();
            Console.ReadLine();
            server.Stop();
        }
    }
}
