﻿using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BlockChainP2P.P2PNetwork.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BlockChainController : ControllerBase
{
    private readonly IBlockChainManager _blockChainManager;
    private readonly IBlockChainData _blockChainData;
    private readonly ITransactionPoolBroadcastManager _transactionPoolBroadcastManager;

    public BlockChainController(
        IBlockChainManager blockChainManager,
        IBlockChainData blockChainData,
        ITransactionPoolBroadcastManager transactionPoolBroadcastManager)
    {
        _blockChainManager = blockChainManager;
        _blockChainData = blockChainData;
        _transactionPoolBroadcastManager = transactionPoolBroadcastManager;
    }

    /// <summary>
    /// Gets this node blockchain
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetBlockchain")]
    [ProducesResponseType(typeof(List<BlockLib>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlockchainAsync()
    {
        var result = await _blockChainData.GetBlockChainAsync();
        return Ok(result);
    }
    
    /// <summary>
    /// Gets this node unspent transaction outs
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetAvailableTxOuts")]
    [ProducesResponseType(typeof(List<UnspentTransactionOutput>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableTxOuts()
    {
        var result = await _blockChainManager.GetAvailableUnspentTxOuts();
        return Ok(result);
    }

    ///// <summary>
    ///// Creates new block
    ///// </summary>
    ///// <param name="data">Data for the new block</param>
    ///// <returns></returns>
    //[HttpPost("CreateNextBlock")]
    //[ProducesResponseType(typeof(BlockLib), StatusCodes.Status201Created)]
    //public async Task<IActionResult> CreateNextBlockAsync([FromBody] List<TransactionLib> request)
    //{
    //    var result = await _blockChainManager.GenerateNextBlockAsync(request);
    //    return Ok(result);
    //}

    [HttpPost("ReceiveTransaction")]
    public async Task<IActionResult> ReceiveTransaction([FromBody] TransactionLib newTransaction)
    {
        
        var result = await _transactionPoolBroadcastManager.ReceiveTransactions(new List<TransactionLib>() { newTransaction });
        return Ok(result);
    }

    [HttpPost("ReceiveTransactions")]
    public async Task<IActionResult> ReceiveTransactions([FromBody] List<TransactionLib> newTransactions)
    {
        
        var result = await _transactionPoolBroadcastManager.ReceiveTransactions(newTransactions);
        return Ok(result);
    }

    [HttpPost("EvilTransaction")]
     public async Task<IActionResult> EvilTransaction([FromBody] TransactionLib evilTransaction)
    {
        
        await _transactionPoolBroadcastManager.AddNewTxPoolAsync(new List<TransactionLib>() { evilTransaction });
        return Ok();
    }

    [HttpPost("EvilBlockchain")]
     public async Task<IActionResult> EvilBlockchain([FromBody] List<BlockLib> evilBlockchain)
    {
        
        _blockChainData.SwapBlockChainsAsync(evilBlockchain);
        return Ok();
    }
}
