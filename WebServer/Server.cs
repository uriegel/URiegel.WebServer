using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using UwebServer.Routes;

namespace UwebServer
{
    public class Server
    {
        public Settings Settings { get; }

        public bool IsStarted { get; private set; }

        internal ReadOnlyDictionary<string, Route[]> Routes { get; }

        public Server(Settings settings)
        {
            Settings = settings;
            if (string.IsNullOrEmpty(Settings.DomainName))
                Settings.DomainName = Dns.GetHostEntry(Environment.MachineName).HostName;

            var routes = from n in Settings.Routes
                         group n by n.Host into v
                         select new { key = v.Key ?? "", value = v.ToArray() };
            Routes = new(routes.ToDictionary(n => n.key, n => n.value));
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

                if (Settings.IsTlsEnabled)
                {
                    Console.WriteLine("Initializing TLS");
                    Settings.Certificate = new X509Certificate2(Path.Combine(Settings.EncryptDirectory, "certificate.pfx"), "uriegel");
                    Console.WriteLine($"Using certificate: {Settings.Certificate}");
                    if (Settings.CheckRevocation)
                        Console.WriteLine("Checking revocation lists");

                    Console.WriteLine($"Listening on secure port {Settings.TlsPort}");
                    result = Ipv6TcpListenerFactory.Create(Settings.TlsPort);
                    tlsListener = result.Listener;
                    if (!result.Ipv6)
                        Console.WriteLine("IPv6 or IPv6 dual mode not supported, switching to IPv4");

                    Console.WriteLine("TLS initialized");
                }

                if (listener != null)
                {
                    Console.WriteLine("Starting HTTP listener...");
                    listener.Start();
                    Console.WriteLine("HTTP listener started");
                }
                if (tlsListener != null)
                {
                    Console.WriteLine("Starting HTTPS listener...");
                    tlsListener.Start();
                    Console.WriteLine("HTTPS listener started");
                }
                IsStarted = true;
                if (listener != null)
                    StartConnecting(listener, false);
                if (tlsListener != null)
                    StartConnecting(tlsListener, true);

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
                if (tlsListener != null)
                {
                    Console.WriteLine("Stopping HTTPS listener...");
                    tlsListener.Stop();
                    Console.WriteLine("HTTPS listener stopped");
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
    }
}
