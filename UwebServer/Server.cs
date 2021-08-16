using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UwebServer
{
    public class Server
    {
        public Settings Settings { get; }

        public bool IsStarted { get; private set; }
        
        public Server(Settings settings)
        {
            Settings = settings;
            if (string.IsNullOrEmpty(Settings.DomainName))
                Settings.DomainName = Dns.GetHostEntry(Environment.MachineName).HostName;

        }

        public void Start()
        {
            try 
            {
                Console.WriteLine("Starting server...");
                Console.WriteLine($"Configuration: {Settings}");
                var result = Ipv6TcpListenerFactory.Create(Settings.Port);
                listener = result.Listener;
                if (!result.Ipv6)
                    Console.WriteLine("IPv6 or IPv6 dual mode not supported, switching to IPv4");

                if (listener != null)
                {
                    Console.WriteLine("Starting HTTP listener...");
                    listener.Start();
                    Console.WriteLine("HTTP listener started");
                }
                IsStarted = true;

                if (listener != null)
                    StartConnecting(listener, false);

                Console.WriteLine("Server started");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not start HTTP(S) Listener: {e}");
                IsStarted = false;
                throw;
            }            
        }

        public void Stop()
        {
            try 
            {
                Console.WriteLine("Stopping server...");
                IsStarted = false;
                if (listener != null)
                {
                    Console.WriteLine("Stopping HTTP listener...");
                    listener.Stop();
                    Console.WriteLine("HTTP listener stopped");
                }

                Console.WriteLine("Server stopped");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Could not stop web server: {e}");
            }
        }

        void StartConnecting(TcpListener listener, bool isSecured)
        {
            if (!IsStarted)
                return;

            new Thread(() =>
            {
                try
                {
                    while (IsStarted)
                    {
						var client = listener.AcceptTcpClient();
						OnConnected(client, isSecured); 
					}
                }
                catch (SocketException se) when (se.SocketErrorCode == SocketError.Interrupted && !IsStarted)
                {
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error occurred in connecting thread: {e}");
                }
            })
            {
                IsBackground = true
            }.Start();
        }

		void OnConnected(TcpClient tcpClient, bool isSecured)
        {
            try
            {
                if (!IsStarted)
                    return;
                SocketSession.StartReceiving(this, tcpClient, isSecured);
            }
            catch (SocketException se) when (se.NativeErrorCode == 10054)
            { }
            catch (ObjectDisposedException)
            {
                // Stop() aufgerufen
                return;
            }
            catch (Exception e)
            {
                if (!IsStarted)
                    return;
                Console.Error.WriteLine($"Error in OnConnected occurred: {e}");
            }
        }

        static Server()
            => ServicePointManager.DefaultConnectionLimit = 1000;

		TcpListener listener;
		TcpListener tlsListener;
		TcpListener tlsRedirectorListener;
    }
}
