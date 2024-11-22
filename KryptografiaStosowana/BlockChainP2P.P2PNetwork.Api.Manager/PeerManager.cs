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

namespace BlockChainP2P.P2PNetwork.Api.Manager;

internal class PeerManager : IPeerManager
{
    private readonly IPeerData _peerData;
    private readonly IServiceProvider _serviceProvider;
    private IBlockChainManager? _blockChainManager;

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

            AdjustConnection(connection, thisNode);
            
            await connection.StartAsync();

            await connection.InvokeAsync("RegisterPeer", thisNode);

            peerToConnect.ConnectionId = connection.ConnectionId;
            await _peerData.AddPeerToKnownPeersAsync(peerToConnect);
            await _peerData.AddHubConnection(connectionKey, connection);

            Log.Information($"Successfully connected to peer {connectionKey}");

            await _blockChainManager.RequestAndUpdateBlockchainAsync(connection);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to connect to peer {connectionKey}: {ex.Message}");
            return false;
        }
    }
#region PrivateCon
    // wyniosłem tutaj żeby było wygodniej, dodałem do PeerJoined metodę odpowiedzialną za połączenie innych nodów z nowym nodem
    private void AdjustConnection(HubConnection connection, PeerLib thisNode)
    {
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

        connection.On<PeerLib>("PeerJoined", async (peerToConnectTo) =>
        {
            if (peerToConnectTo.IPAddress != thisNode.IPAddress || peerToConnectTo.Port != thisNode.Port)
            {
                await _peerData.AddPeerToKnownPeersAsync(peerToConnectTo);
                await ConnectToNewPeerAsync(peerToConnectTo); // to nowe, nie jestem pewien czy tu definiujemy metody dla huba odbierającego czy dla tego co się z nim łączymy
                                                              // ta metoda musi być w miejscu takim, że od razu po otrzymaniu info przez noda że jest nowy peer to to powinno
                                                              // się zrobić automatycznie.
                Log.Information($"New peer joined: {peerToConnectTo.IPAddress}:{peerToConnectTo.Port}");
            }
        });

        connection.On<BlockLib>("ReceiveNewBlock", async (block) =>
        {
            Log.Information($"Received new block with index {block.Index}");
            await _blockChainManager.ReceiveNewBlockAsync(block);
        });
    }

    private async Task ConnectToNewPeerAsync(PeerLib peerToConnectTo)
    {
        var thisNode = await _peerData.GetThisPeerInfoAsync();
        if (peerToConnectTo.IPAddress == thisNode.IPAddress && peerToConnectTo.Port == thisNode.Port)
        {
            Log.Information("Skipping connection attempt to self");
            return;
        }

        string connectionKey = $"{peerToConnectTo.IPAddress}:{peerToConnectTo.Port}";
        if (await _peerData.IsConnectedToPeer(connectionKey))
        {
            Log.Information($"Already connected to peer {connectionKey}");
            return;
        }

        try
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"http://{peerToConnectTo.IPAddress}:{peerToConnectTo.Port}/blockchainHub")
                .WithAutomaticReconnect()
                .Build();
            //Trochę nie kumam co się dzieje w tym hubie, należy wrzucić tą metodę (ConnectToNewPeerAsync)
            //łączenia do nowego peera w miejsce gdzie istniejący w sieci peer
            //dostaje info o nowym peerze. Wtedy przygotowuje connecta do jego huba i po prostu robi connection.StartAsync() i zapisuje do swoich hubconnection
            AdjustConnection(connection, thisNode);

            await connection.StartAsync();

            peerToConnectTo.ConnectionId = connection.ConnectionId;
            await _peerData.AddHubConnection(connectionKey, connection);

            Log.Information($"Successfully connected to peer {connectionKey}");
            return;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to connect to peer {connectionKey}: {ex.Message}");
            return;
        }
    }

#endregion PrivateCon
    public async Task RegisterPeerAsync(PeerLib peer)
    {
        var connection = new HubConnectionBuilder()
                .WithUrl($"http://{peer.IPAddress}:{peer.Port}/blockchainHub")
                .WithAutomaticReconnect()
                .Build();
        
        await _peerData.AddPeerToKnownPeersAsync(peer);
        
        var knownPeers = await _peerData.GetAllKnownPeersAsync();
        await BroadcastToPeers("ReceiveKnownPeers", knownPeers);
        
        Log.Information($"Successfully registered new peer with IP address: {peer.IPAddress} and port number: {peer.Port}");
    }

    public async Task RemovePeerAsync(string connectionId)
    {
        await _peerData.RemoveHubConnection(connectionId);
        Log.Information($"Peer disconnected: {connectionId}");
    }

    public async Task BroadcastToPeers<T>(string method, T data)
    {
        var connections = await _peerData.GetAllConnectionsAsync();
        Log.Information($"Broadcasting {method} to {connections.Count} peers {string.Join(", ", connections.Keys)}");
        var tasks = new List<Task>();
        
        // lock (_connectionsLock) czy jest tu potrzebny?
        foreach (var connection in connections.Values)
        {
            if (connection.State == HubConnectionState.Connected)
            {
                Log.Information($"Sending {method} to {connection.ConnectionId}");
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
            
            // Próbujemy nawiązać połączenie z nowym peerem
            await ConnectWithPeerNetworkAsync(peer);
        }

        Log.Information($"Zarejestrowano {peers.Count} nowych peerów");
    }
}
