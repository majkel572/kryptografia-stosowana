using Microsoft.AspNetCore.SignalR;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Serilog;

namespace BlockChainP2P.P2PNetwork.Api.Hubs;

public class BlockchainHub : Hub
{
    private readonly IPeerManager _peerManager;
    private readonly IPeerData _peerData;
    private readonly IBlockChainManager _blockChainManager;
    private readonly IBlockChainData _blockChainData;
    private readonly ITransactionPool _transactionPool;
    private readonly ITransactionPoolBroadcastManager _transactionManager;
    public BlockchainHub(IPeerManager peerManager, IBlockChainManager blockChainManager, IBlockChainData blockChainData, IPeerData peerData, ITransactionPool transactionPool, ITransactionPoolBroadcastManager transactionManager)
    {
        _peerManager = peerManager;
        _blockChainManager = blockChainManager;
        _blockChainData = blockChainData;
        _peerData = peerData;
        _transactionPool = transactionPool;
        _transactionManager = transactionManager;
    }

    public async Task RegisterPeer(PeerLib peer)
    {
        peer.ConnectionId = Context.ConnectionId;
        await _peerManager.RegisterPeerAsync(peer);
        
        // Pobierz listę znanych peerów
        var knownPeers = await _peerManager.GetKnownPeersAsync();
        
        // Wyślij listę znanych peerów do nowego peera
        // await Clients.Caller.SendAsync("ReceiveKnownPeers", knownPeers);
        
        // // Powiadom innych o nowym peerze
        // await Clients.Others.SendAsync("PeerJoined", peer);
    }

    public async Task RequestConnectionWithPeer(PeerLib peer) {
        await _peerManager.RequestConnectionWithPeerAsync(peer);
    }

    public async Task ReceiveKnownPeers(List<PeerLib> peers)
    {
        await _peerManager.RegisterPeersAsync(peers);
    }

    public async Task ReceiveNewBlock(BlockLib newBlock)
    {
        var httpContext = Context.GetHttpContext();
        var clientIp = httpContext?.Connection?.RemoteIpAddress?.ToString();
        
        var result =await _blockChainManager.ReceiveNewBlockAsync(newBlock);
        if (!result && clientIp != null) {
            string connectionKey = $"{clientIp}:8080";
            var connection = await _peerData.GetHubConnection(connectionKey);
            if (connection != null) {
                await _blockChainManager.RequestAndUpdateBlockchainAsync(connection);
            }
        }
    }

    public async Task RequestBlockchain()
    {
        var blockchain = await _blockChainData.GetBlockChainAsync();
        await Clients.Caller.SendAsync("ReceiveBlockchain", blockchain);
    }

    public async Task RequestTxPool()
    {
        var transactions = await _transactionPool.GetTransactions();
        Log.Information($"liczba transakcji w oryginale: {transactions.Count}");
        await Clients.Caller.SendAsync("ReceiveTxPool", transactions);
    }

    public async Task ReceiveBlockchain(IEnumerable<BlockLib> blockchain)
    {
        _blockChainManager.ReplaceBlockChain(blockchain.ToList());
    }

    public async Task ReceiveTxPool(IEnumerable<TransactionLib> transactions)
    {
        await _transactionManager.AddNewTxAsync(transactions.ToList());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _peerManager.RemovePeerAsync(Context.ConnectionId);
        // await base.OnDisconnectedAsync(exception);
    }
} 