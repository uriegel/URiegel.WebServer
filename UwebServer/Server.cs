using System;
using System.Net;

namespace UwebServer
{
    public class Server
    {
        public Settings Settings { get; }
        
        public Server(Settings settings)
        {
            Settings = settings;
            if (string.IsNullOrEmpty(Settings.DomainName))
                Settings.DomainName = Dns.GetHostEntry(Environment.MachineName).HostName;

        }

        public void Start()
        {
            Console.WriteLine("Starting server...");
            Console.WriteLine($"Configuration: {Settings}");
            Console.WriteLine("Server started");
        }

        public void Stop()
        {
            Console.WriteLine("Stopping server...");
            Console.WriteLine("Server stopped");
        }

        static Server()
            => ServicePointManager.DefaultConnectionLimit = 1000;
    }
}
