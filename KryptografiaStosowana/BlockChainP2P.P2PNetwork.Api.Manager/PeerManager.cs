using BlockChainP2P.P2PNetwork.Api.Lib;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using System.Text.Json;
using System.Text;
using Serilog;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlockChainP2P.P2PNetwork.Api.Manager;

internal class PeerManager : IPeerManager
{
    private readonly IPeerData _peerData;
    private readonly Dictionary<string, HubConnection> _connections;
    private readonly object _connectionsLock = new object();

    public PeerManager(IPeerData peerData)
    {
        _peerData = peerData ?? throw new ArgumentNullException(nameof(peerData));
        _connections = new Dictionary<string, HubConnection>();
    }

    public async Task<bool> ConnectWithPeerNetworkAsync(PeerLib peerToConnect)
    {
        var thisNode = await _peerData.GetThisPeerInfoAsync();
        if (peerToConnect.IPAddress == thisNode.IPAddress && peerToConnect.Port == thisNode.Port)
        {
            Log.Information("Skipping connection attempt to self");
            return false;
        }

        string connectionKey = $"{peerToConnect.IPAddress}:{peerToConnect.Port}";
        lock (_connectionsLock)
        {
            if (_connections.ContainsKey(connectionKey))
            {
                Log.Information($"Already connected to peer {connectionKey}");
                return true;
            }
        }

        try
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"http://{peerToConnect.IPAddress}:{peerToConnect.Port}/blockchainHub")
                .WithAutomaticReconnect()
                .Build();

            connection.On<List<PeerLib>>("ReceiveKnownPeers", async (peers) =>
            {
                Log.Information($"Received list of {peers.Count} known peers");
                var connectTasks = new List<Task>();
                
                foreach (var peer in peers.Where(p => 
                    p.IPAddress != thisNode.IPAddress || p.Port != thisNode.Port))
                {
                    await _peerData.AddPeerToKnownPeersAsync(peer);
                    connectTasks.Add(ConnectWithPeerNetworkAsync(peer));
                }
                
                await Task.WhenAll(connectTasks);
            });

            connection.On<PeerLib>("PeerJoined", async (peer) =>
            {
                if (peer.IPAddress != thisNode.IPAddress || peer.Port != thisNode.Port)
                {
                    await _peerData.AddPeerToKnownPeersAsync(peer);
                    await _peerData.AddPeerToWorkingPeersAsync(peer);
                    Log.Information($"New peer joined: {peer.IPAddress}:{peer.Port}");
                }
            });
            
            await connection.StartAsync();
            
            lock (_connectionsLock)
            {
                _connections[connectionKey] = connection;
            }

            await connection.InvokeAsync("RegisterPeer", thisNode);

            peerToConnect.ConnectionId = connection.ConnectionId;
            await _peerData.AddPeerToKnownPeersAsync(peerToConnect);
            await _peerData.AddPeerToWorkingPeersAsync(peerToConnect);

            Log.Information($"Successfully connected to peer {connectionKey}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to connect to peer {connectionKey}: {ex.Message}");
            return false;
        }
    }

    public async Task RegisterPeerAsync(PeerLib peer, string connectionId)
    {
        peer.ConnectionId = connectionId;
        await _peerData.AddPeerToKnownPeersAsync(peer);
        await _peerData.AddPeerToWorkingPeersAsync(peer);
        
        var knownPeers = await _peerData.GetAllKnownPeersAsync();
        await BroadcastToPeers("ReceiveKnownPeers", knownPeers);
        
        Log.Information($"Successfully registered new peer with IP address: {peer.IPAddress} and port number: {peer.Port}");
    }

    public async Task RemovePeerAsync(string connectionId)
    {
        var peer = await _peerData.GetPeerByConnectionIdAsync(connectionId);
        if (peer != null)
        {
            await _peerData.DeletePeerFromWorkingPeersAsync(peer.IPAddress, peer.Port);
            lock (_connectionsLock)
            {
                _connections.Remove(peer.IPAddress + ":" + peer.Port);
            }
            Log.Information($"Peer disconnected: {peer.IPAddress}:{peer.Port}");
        }
    }

    public async Task BroadcastToPeers<T>(string method, T data)
    {
        var tasks = new List<Task>();
        
        lock (_connectionsLock)
        {
            foreach (var connection in _connections.Values)
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    tasks.Add(connection.InvokeAsync(method, data).ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            Log.Error($"Failed to broadcast {method}: {t.Exception?.GetBaseException().Message}");
                        }
                    }));
                }
            }
        }

        await Task.WhenAll(tasks);
    }

    public async Task<List<PeerLib>> GetKnownPeersAsync()
    {
        return await _peerData.GetAllKnownPeersAsync();
    }

    public async Task RegisterPeersAsync(List<PeerLib> peers)
    {
        if (peers == null || !peers.Any())
        {
            return;
        }

        var thisNode = await _peerData.GetThisPeerInfoAsync();
        
        foreach (var peer in peers)
        {
            // Pomijamy samego siebie
            if (peer.IPAddress == thisNode.IPAddress && peer.Port == thisNode.Port)
            {
                continue;
            }

            await _peerData.AddPeerToKnownPeersAsync(peer);
            await _peerData.AddPeerToWorkingPeersAsync(peer);
            
            // Próbujemy nawiązać połączenie z nowym peerem
            await ConnectWithPeerNetworkAsync(peer);
        }

        Log.Information($"Zarejestrowano {peers.Count} nowych peerów");
    }
}
