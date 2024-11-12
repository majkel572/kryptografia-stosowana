using Microsoft.AspNetCore.SignalR;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;

namespace BlockChainP2P.P2PNetwork.Api.Hubs;

public class BlockchainHub : Hub
{
    private readonly IPeerManager _peerManager;

    public BlockchainHub(IPeerManager peerManager)
    {
        _peerManager = peerManager;
    }

    public async Task RegisterPeer(PeerLib peer)
    {
        await _peerManager.RegisterPeerAsync(peer, Context.ConnectionId);
        
        // Pobierz listę znanych peerów
        var knownPeers = await _peerManager.GetKnownPeersAsync();
        
        // Wyślij listę znanych peerów do nowego peera
        await Clients.Caller.SendAsync("ReceiveKnownPeers", knownPeers);
        
        // Powiadom innych o nowym peerze
        await Clients.Others.SendAsync("PeerJoined", peer);
    }

    public async Task ReceiveKnownPeers(List<PeerLib> peers)
    {
        await _peerManager.RegisterPeersAsync(peers);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _peerManager.RemovePeerAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
} 