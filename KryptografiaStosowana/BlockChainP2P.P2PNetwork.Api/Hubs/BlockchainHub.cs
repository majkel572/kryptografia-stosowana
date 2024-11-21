using Microsoft.AspNetCore.SignalR;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;

namespace BlockChainP2P.P2PNetwork.Api.Hubs;

public class BlockchainHub : Hub
{
    private readonly IPeerManager _peerManager;
    private readonly IBlockChainManager _blockChainManager;
    private readonly IBlockChainData _blockChainData;
    public BlockchainHub(IPeerManager peerManager, IBlockChainManager blockChainManager, IBlockChainData blockChainData)
    {
        _peerManager = peerManager;
        _blockChainManager = blockChainManager;
        _blockChainData = blockChainData;
    }

    public async Task RegisterPeer(PeerLib peer)
    {
        peer.ConnectionId = Context.ConnectionId;
        await _peerManager.RegisterPeerAsync(peer);
        
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

    public async Task ReceiveNewBlock(BlockLib newBlock)
    {
        await _blockChainManager.ReceiveNewBlockAsync(newBlock);
    }

    public async Task RequestBlockchain()
    {
        var blockchain = await _blockChainData.GetBlockChainAsync();
        await Clients.Caller.SendAsync("ReceiveBlockchain", blockchain);
    }

    public async Task ReceiveBlockchain(IEnumerable<BlockLib> blockchain)
    {
        _blockChainManager.ReplaceBlockChain(blockchain.ToList());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _peerManager.RemovePeerAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
} 