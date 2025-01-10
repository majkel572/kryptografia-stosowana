using BlockChainP2P.P2PNetwork.Api.Lib;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using System.Text.Json;
using System.Text;
using Serilog;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Peers;

internal class PeerManager : IPeerManager
{
    private readonly IPeerData _peerData;
    private readonly IServiceProvider _serviceProvider;
    private IBlockChainManager? _blockChainManager;
    private ITransactionPoolBroadcastManager? _transactionPoolManager;

    public PeerManager(
        IPeerData peerData,
        IServiceProvider serviceProvider)
    {
        _peerData = peerData ?? throw new ArgumentNullException(nameof(peerData));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private IBlockChainManager BlockChainManager
    {
        get
        {
            if (_blockChainManager == null)
            {
                _blockChainManager = _serviceProvider.GetRequiredService<IBlockChainManager>();
            }
            return _blockChainManager;
        }
    }
    private ITransactionPoolBroadcastManager TransactionPoolManager
    {
        get
        {
            if (_transactionPoolManager == null)
            {
                _transactionPoolManager = _serviceProvider.GetRequiredService<ITransactionPoolBroadcastManager>();
            }
            return _transactionPoolManager;
        }
    }

    public async Task<bool> ConnectWithPeerNetworkAsync(PeerLib peerToConnect)
    {
        var connections = await _peerData.GetAllConnectionsAsync();
        var thisNode = await _peerData.GetThisPeerInfoAsync();
        if (peerToConnect.IPAddress == thisNode.IPAddress && peerToConnect.Port == thisNode.Port)
        {
            Log.Information("Skipping connection attempt to self");
            return false;
        }

        string connectionKey = $"{peerToConnect.IPAddress}:{peerToConnect.Port}";
        if (await _peerData.IsConnectedToPeer(connectionKey))
        {
            Log.Information($"Already connected to peer {connectionKey}");
            return true;
        }

        try
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"http://{peerToConnect.IPAddress}:{peerToConnect.Port}/blockchainHub")
                .WithAutomaticReconnect()
                .Build();

            // connection.On<List<PeerLib>>("ReceiveKnownPeers", async (peers) =>
            // {
            //     Log.Information($"Received list of {peers.Count} known peers");
            //     var connectTasks = new List<Task>();

            //     foreach (var peer in peers.Where(p => 
            //         p.IPAddress != thisNode.IPAddress || p.Port != thisNode.Port))
            //     {
            //         await _peerData.AddPeerToKnownPeersAsync(peer);
            //         connectTasks.Add(ConnectWithPeerNetworkAsync(peer));
            //     }

            //     await Task.WhenAll(connectTasks);
            // });

            // connection.On<PeerLib>("PeerJoined", async (peer) =>
            // {
            //     if (peer.IPAddress != thisNode.IPAddress || peer.Port != thisNode.Port)
            //     {
            //         await _peerData.AddPeerToKnownPeersAsync(peer);
            //         Log.Information($"New peer joined: {peer.IPAddress}:{peer.Port}");
            //     }
            // });

            // connection.On<BlockLib>("ReceiveNewBlock", async (block) =>
            // {
            //     Log.Information($"Received new block with index {block.Index}");
            //     await _blockChainManager.ReceiveNewBlockAsync(block);
            // });

            await connection.StartAsync();

            // await connection.InvokeAsync("RegisterPeer", thisNode);

            peerToConnect.ConnectionId = connection.ConnectionId;
            await _peerData.AddPeerToKnownPeersAsync(peerToConnect);
            await _peerData.AddHubConnection(connectionKey, connection);

            connection.InvokeAsync("RegisterPeer", thisNode);
            Log.Information($"Successfully connected to peer {connectionKey}");

            BlockChainManager.RequestAndUpdateBlockchainAsync(connection);
            TransactionPoolManager.RequestAndUpdateTxPoolAsync(connection);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to connect to peer {connectionKey}: {ex.Message}");
            return false;
        }
    }

    public async Task RegisterPeerAsync(PeerLib peer)
    {
        var connection = await MakeConnectionWithPeer(peer);
        if (connection == null)
        {
            return;
        }

        var knownPeers = await _peerData.GetAllKnownPeersAsync();
        await connection.InvokeAsync("ReceiveKnownPeers", knownPeers);
    }

    public async Task RemovePeerAsync(string connectionId)
    {
        await _peerData.RemoveHubConnection(connectionId);
        Log.Information($"Peer disconnected: {connectionId}");
    }

    public async Task BroadcastToPeers<T>(string method, T data)
    {
        if(!_peerData.IsBroadcasting())
        {
            Log.Information("broadcast is off, skipping");
            return;
        }
        var connections = await _peerData.GetAllConnectionsAsync();
        Log.Information($"Broadcasting {method} to {connections.Count} peers {string.Join(", ", connections.Keys)}");
        var tasks = new List<Task>();

        // lock (_connectionsLock) czy jest tu potrzebny?
        foreach (var connectionPair in connections)
        {
            var connection = connectionPair.Value;
            if (connection.State == HubConnectionState.Connected)
            {
                Log.Information($"Sending {method} to {connectionPair.Key}");
                tasks.Add(connection.InvokeAsync(method, data).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Log.Error($"Failed to broadcast {method}: {t.Exception?.GetBaseException().Message}");
                    }
                }));
            }
        }
        await Task.WhenAll(tasks);
    }

    public async Task<List<PeerLib>> GetKnownPeersAsync()
    {
        return await _peerData.GetAllKnownPeersAsync();
    }

    public async Task RequestConnectionWithPeerAsync(PeerLib peer)
    {
        await MakeConnectionWithPeer(peer);
        return;
    }

    public async Task RegisterPeersAsync(List<PeerLib> peers)
    {
        if (peers == null || !peers.Any())
        {
            return;
        }

        var thisNode = await _peerData.GetThisPeerInfoAsync();
        var knownPeers = await _peerData.GetAllKnownPeersAsync();
        int newPeersCount = 0;

        foreach (var peer in peers)
        {
            // Pomijamy samego siebie
            if (peer.IPAddress == thisNode.IPAddress && peer.Port == thisNode.Port)
            {
                continue;
            }

            if (knownPeers.Any(p => p.IPAddress == peer.IPAddress && p.Port == peer.Port))
            {
                continue;
            }

            // Próbujemy nawiązać połączenie z nowym peerem
            var connection = await MakeConnectionWithPeer(peer);
            if (connection == null)
            {
                continue;
            }

            connection.InvokeAsync("RequestConnectionWithPeer", thisNode);
            newPeersCount++;
        }

        Log.Information($"Zarejestrowano {newPeersCount} nowych peerów");
    }

    private async Task<HubConnection?> MakeConnectionWithPeer(PeerLib peer)
    {
        const int maxRetries = 3;
        const int delayMilliseconds = 2000; // 2 sekundy między próbami

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl($"http://{peer.IPAddress}:{peer.Port}/blockchainHub")
                    .WithAutomaticReconnect()
                    .Build();

                await connection.StartAsync();

                string connectionKey = $"{peer.IPAddress}:{peer.Port}";
                peer.ConnectionId = connection.ConnectionId;
                await _peerData.AddHubConnection(connectionKey, connection);
                await _peerData.AddPeerToKnownPeersAsync(peer);

                Log.Information($"Pomyślnie zarejestrowano nowego peera (IP: {peer.IPAddress}, port: {peer.Port}) przy próbie {attempt}");
                return connection; // Sukces - wychodzimy z metody
            }
            catch (Exception ex)
            {
                Log.Warning($"Próba {attempt}/{maxRetries} połączenia z peerem (IP: {peer.IPAddress}, port: {peer.Port}) nie powiodła się: {ex.Message}");

                if (attempt < maxRetries)
                {
                    await Task.Delay(delayMilliseconds);
                    continue;
                }

                Log.Error($"Nie udało się zarejestrować peera po {maxRetries} próbach");
            }
        }

        return null;
    }
}
