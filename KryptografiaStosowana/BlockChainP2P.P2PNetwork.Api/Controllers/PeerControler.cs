using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BlockChainP2P.P2PNetwork.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PeerController : ControllerBase
{
    private readonly IPeerManager _peerManager;
    private readonly ITransactionPoolBroadcastManager _transactionPool;
    private readonly IUnspentTransactionOutData _unspentTransactionOutData;
    private readonly IPeerData _peerData;

    public PeerController(IPeerManager peerManager, ITransactionPoolBroadcastManager transactions, IUnspentTransactionOutData unspentTransactionOutData, IPeerData peerData)
    {
        _peerManager = peerManager;
        _transactionPool = transactions;
        _unspentTransactionOutData = unspentTransactionOutData;
        _peerData = peerData;
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectToPeer([FromBody] PeerLib peer)
    {
        var result = await _peerManager.ConnectWithPeerNetworkAsync(peer);
        return result ? Ok() : BadRequest();
    }

    [HttpGet("transactions")]
    public async Task<List<TransactionLib>> GetTransactionsForTest()
    {
        var result = await _transactionPool.GetAllTransactions();
        return result;
    }

    [HttpGet("unspenttransactions")]
    public async Task<List<UnspentTransactionOutput>> GetUnspentTransactionsForTest()
    {
        var result = _unspentTransactionOutData.GetUnspentTxOut();
        return result;
    }

    [HttpPost("addtx")]
    public async Task<IActionResult> AddTransactionsForTest()
    {
        var tmp = new List<TransactionLib> { new TransactionLib { Id = "transakcja 1", TransactionInputs = new List<TransactionInputLib>(), TransactionOutputs = new List<TransactionOutputLib>()},
        new TransactionLib { Id = "transakcja 2", TransactionInputs = new List<TransactionInputLib>(), TransactionOutputs = new List<TransactionOutputLib>()}};
        await _transactionPool.AddNewTxPoolAsync(tmp);
        return Ok();
    }

    [HttpGet("BroadcastOnOff")]
    public async Task<IActionResult> BroadcastOnOff()
    {

        _peerData.ChangeIsBrodcasting();
        return Ok();
    }
}
