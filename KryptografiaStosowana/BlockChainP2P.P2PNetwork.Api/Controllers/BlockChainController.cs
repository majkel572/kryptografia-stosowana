using BlockChainP2P.P2PNetwork.Api.Lib.Model;
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

    public BlockChainController(
        IBlockChainManager blockChainManager,
        IBlockChainData blockChainData)
    {
        _blockChainManager = blockChainManager;
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
    /// Creates new block
    /// </summary>
    /// <param name="data">Data for the new block</param>
    /// <returns></returns>
    [HttpPost("CreateNextBlock")]
    [ProducesResponseType(typeof(BlockLib), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateNextBlockAsync([FromBody] string request)
    {
        var result = await _blockChainManager.GenerateNextBlockAsync(request);
        return Ok(result);
    }

}
