using BlockChainP2P.P2PNetwork.Interfaces;
using BlockChainP2P.P2PNetwork.Models;
using BlockChainP2P.P2PNetwork.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace P2PNode
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            string localIpAddress = "127.0.0.1";

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IPeerNetwork, PeerNetwork>();
                    services.AddSingleton<IBroadcastService, BroadcastService>();
                    services.AddSingleton<IMessageHandlerService, MessageHandlerService>();
                    services.AddSingleton<IPeerDiscoveryService, PeerDiscoveryService>();
                })
                .Build();

            var peerNetwork = host.Services.GetRequiredService<IPeerNetwork>();

            Console.WriteLine("Starting the node...");

            // Change this line if You implement address IP handling with port - currently it strips port from ip:port arg
            int port = args.Length > 0 && args[0].Contains(":") ? int.Parse(args[0].Split(':')[1]) : 5000;


            if (localIpAddress == null)
            {
                Console.WriteLine("Unable to get local IP address.");
                return;
            }

            while (!TryStartNode(peerNetwork, port))
            {
                port++;
                Console.WriteLine($"Port {port - 1} is in use, trying next port: {port}...");
            }

            if (args.Length > 0 && args[0].Contains(":") || port != 5000)
            {
                string[] ipPort = args[0].Split(':');
                var nodeInfo = new NodeInfo(ipPort[0], int.Parse(ipPort[1]));
                peerNetwork.ConnectToPeer(nodeInfo);
            }

            while (true)
            {
                await Task.Delay(1000); // Czekaj 1 sekundę przed kolejną iteracją
            }

        }

        // Helper method to try starting the node and check if the port is available
        private static bool TryStartNode(IPeerNetwork peerNetwork, int port)
        {
            try
            {
                peerNetwork.StartNode(port);

                // Get the local machine's IP address (IPv4) and confirm the node is running
                string localIpAddress = "127.0.0.1"; // GetLocalIPAddress();
                Console.WriteLine($"Node is successfully running at {localIpAddress}:{port}");
                return true;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    // Port is in use, return false to trigger retry with next port
                    return false;
                }

                // Re-throw for other socket errors
                throw;
            }
        }

        // Helper method to get local IP address (IPv4)
        private static string GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return null;
        }
    }
}
