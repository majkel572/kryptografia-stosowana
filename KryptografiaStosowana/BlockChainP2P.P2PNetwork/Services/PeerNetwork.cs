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
    internal class PeerNetwork : IPeerNetwork
    {
        private readonly List<NodeInfo> _connectedPeers;
        private readonly IMessageHandlerService _messageHandlerService;
        private TcpListener _listener;
        private readonly List<NodeInfo> _knownPeers;

        private IPAddress _defaultIP => IPAddress.Parse("127.0.0.1");

        public event Action<Message> OnMessageReceived;

        public PeerNetwork(IMessageHandlerService messageHandlerService)
        {
            _connectedPeers = new List<NodeInfo>();
            _messageHandlerService = messageHandlerService;
            _knownPeers = new List<NodeInfo>(); 
        }

        public void StartNode(int port)
        {
            _listener = new TcpListener(_defaultIP, port);
            _listener.Start();

            Task.Run(async () =>
            {
                while (true)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Task.Run(async () => HandleClient(client));
                }
            });
        }

        public void ConnectToPeer(NodeInfo nodeInfo)
        {
            if (!_knownPeers.Any(p => p.Address == nodeInfo.Address && p.Port == nodeInfo.Port))
            {
                Console.WriteLine($"Connecting to new peer: {nodeInfo.Address}:{nodeInfo.Port}");
                if(!_knownPeers.Any(x => x.Port == nodeInfo.Port && x.Address == nodeInfo.Address))
                {
                    _knownPeers.Add(nodeInfo);
                }

                var client = new TcpClient(nodeInfo.Address, nodeInfo.Port);
                nodeInfo.Connection = client;
                if (_knownPeers.Any(x => x.Port == nodeInfo.Port && x.Address == nodeInfo.Address))
                {
                    var toRemove = _knownPeers.FirstOrDefault(x => x.Address == nodeInfo.Address && x.Port == nodeInfo.Port);
                    try
                    {
                        toRemove.Connection.Close();
                    }
                    catch (Exception e) { }
                    _knownPeers.Remove(toRemove);
                }

                _connectedPeers.Add(nodeInfo);

                if (!_knownPeers.Any(x => x.Port == nodeInfo.Port && x.Address == nodeInfo.Address))
                {
                    NotifyPeersAboutNewNode(nodeInfo);
                }
            }
        }

        public void SendBCastMessage(Message message)
        {
            foreach (var peer in _connectedPeers)
            {
                SendMessageToPeer(peer, message);
            }
        }

        public void SendMessageToPeer(NodeInfo peer, Message message)
        {
            try
            {
                using (var client = new TcpClient(peer.Address, peer.Port))
                {
                    var stream = client.GetStream();

                    var messageBytes = Encoding.UTF8.GetBytes($"{message.Command}|{message.Payload}");

                    stream.Write(messageBytes, 0, messageBytes.Length);
                    Console.WriteLine($"Sent message to {peer.Address}:{peer.Port}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to {peer.Address}:{peer.Port}: {ex.Message}");
            }
        }

        private async void HandleClient(TcpClient client)
        {
            //using (client)
            //{
            //    NetworkStream stream = client.GetStream();
            //    byte[] buffer = new byte[1024];
            //    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            //    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            //    Console.WriteLine($"Otrzymano wiadomość: {message}");

            //    byte[] response = Encoding.UTF8.GetBytes("Wiadomość odebrana.");
            //    await stream.WriteAsync(response, 0, response.Length);
            //}
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        var messageString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var message = ParseMessage(messageString);

                        if (message.Command == "NewNode")
                        {
                            var nodeParts = message.Payload.Split(':');
                            var newNodeInfo = new NodeInfo(nodeParts[0], int.Parse(nodeParts[1]));
                            ConnectToPeer(newNodeInfo);
                        }

                        _messageHandlerService.HandleMessage(message);
                        OnMessageReceived?.Invoke(message);
                    }
                }
            } catch(Exception ex)
            {
                Console.WriteLine($"Connection to peer lost while trying to decode his message. {ex.Message}");
            }
        }

        private void NotifyPeersAboutNewNode(NodeInfo newNode)
        {
            foreach (var peer in _connectedPeers)
            {
                //if (peer.Address != newNode.Address || peer.Port != newNode.Port)
                //{
                    Console.WriteLine($"Notifying peer {peer.Address}:{peer.Port} about new node {newNode.Address}:{newNode.Port}");
                    var message = new Message("NewNode", $"{newNode.Address}:{newNode.Port}");
                    
                    SendMessageToPeer(peer, message);
                //}
            }
        }

        private async void Reconnect(TcpClient client)
        {
            var motherServer = _knownPeers.FirstOrDefault();
            if(motherServer == null)
            {
                throw new ArgumentNullException("No mother peer defined.");
            }

            while (true)
            {
                try
                {
                    // Attempt to reconnect
                    var newClient = new TcpClient(motherServer.Address, motherServer.Port);
                    NodeInfo nodeInfo = new NodeInfo(motherServer.Address, motherServer.Port, newClient);
                    _connectedPeers.Add(nodeInfo);
                    HandleClient(newClient); // Handle the new connection
                    break; // Exit the loop if successful
                }
                catch (SocketException)
                {
                    await Task.Delay(5000); // Wait before retrying
                }
            }
        }

        private Message ParseMessage(string messageString)
        {
            var parts = messageString.Split('|');

            if (parts.Length == 2)
            {
                var command = parts[0].Trim();
                var payload = parts[1].Trim();
                return new Message(command, payload);
            }

            Console.WriteLine($"Invalid message format: {messageString}");
            return null; 
        }
    }
}
