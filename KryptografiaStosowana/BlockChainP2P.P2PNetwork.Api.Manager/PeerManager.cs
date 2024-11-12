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
        try
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"http://{peerToConnect.IPAddress}:{peerToConnect.Port}/blockchainHub")
                .WithAutomaticReconnect()
                .Build();

            // Obsługa zdarzeń
            connection.On<PeerLib>("PeerJoined", async (peer) =>
            {
                await _peerData.AddPeerToKnownPeersAsync(peer);
                await _peerData.AddPeerToWorkingPeersAsync(peer);
                Log.Information($"New peer joined: {peer.IPAddress}:{peer.Port}");
            });

            await connection.StartAsync();
            
            var thisNode = await _peerData.GetThisPeerInfoAsync();
            await connection.InvokeAsync("RegisterPeer", thisNode);

            lock (_connectionsLock)
            {
                _connections[peerToConnect.IPAddress + ":" + peerToConnect.Port] = connection;
            }

            Log.Information($"Successfully connected to peer {peerToConnect.IPAddress}:{peerToConnect.Port}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to connect to peer {peerToConnect.IPAddress}:{peerToConnect.Port}: {ex.Message}");
            return false;
        }
    }

    public async Task RegisterPeerAsync(PeerLib peer, string connectionId)
    {
        await _peerData.AddPeerToKnownPeersAsync(peer);
        await _peerData.AddPeerToWorkingPeersAsync(peer);
    }

    public async Task RemovePeerAsync(string connectionId)
    {
        var peer = await _peerData.GetPeerByConnectionIdAsync(connectionId);
        if (peer != null)
        {
            await _peerData.DeletePeerFromWorkingPeersAsync(peer.IPAddress, peer.Port);
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
                    tasks.Add(connection.InvokeAsync(method, data));
                }
            }
        }

        await Task.WhenAll(tasks);
    }
}
