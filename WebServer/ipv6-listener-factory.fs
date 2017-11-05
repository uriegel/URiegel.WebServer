module IPV6ListenerFactory
open System.Net
open System.Net.Sockets

type Ipv6Listener = {
    Listener: TcpListener
    Ipv6: bool
}

let create port = 
    try
        let listener = TcpListener(IPAddress.IPv6Any, port) 
        listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0)    
        {
            Listener = listener
            Ipv6 = true
        }
    with
    | :? SocketException as se when se.SocketErrorCode = SocketError.AddressFamilyNotSupported
        ->  let listener = TcpListener(IPAddress.Any, port) 
            {
                Listener = listener
                Ipv6 = false
            }
