using BlockChainP2P.P2PNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlockChainP2P.P2PNetwork.Interfaces;

namespace BlockChainP2P.P2PNetwork.Services
{
    internal class PeerNetwork
    {
        private readonly List<NodeInfo> _connectedPeers;
        private readonly IMessageHandlerService _messageHandlerService;
        private TcpListener _listener;

        public event Action<Message> OnMessageReceived;

        public PeerNetwork(IMessageHandlerService messageHandlerService)
        {
            _connectedPeers = new List<NodeInfo>();
            _messageHandlerService = messageHandlerService;
        }

        public void StartNode(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            Task.Run(async () =>
            {
                while (true)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    HandleClient(client);
                }
            });
        }

        public void ConnectToPeer(NodeInfo nodeInfo)
        {
            var client = new TcpClient(nodeInfo.Address, nodeInfo.Port);
            _connectedPeers.Add(nodeInfo);
            // Handle sending and receiving messages with the connected peer
        }

        public void SendMessage(Message message)
        {
            foreach (var peer in _connectedPeers)
            {
                // Send the message to each connected peer
            }
        }

        private async void HandleClient(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    var messageString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = ParseMessage(messageString); // Implement this method
                    _messageHandlerService.HandleMessage(message);
                    OnMessageReceived?.Invoke(message);
                }
            }
        }

        private Message ParseMessage(string messageString)
        {
            // Parse the incoming message string to create a Message object
            // This will depend on your specific message format
            return new Message("Command", messageString);
        }
    }
}
