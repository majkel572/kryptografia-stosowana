using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;
using BlockChainP2P.P2PNetwork.Api.Lib.Validators;
using BlockChainP2P.WalletHandler.OpenAPI;
using BlockChainP2P.WalletHandler.WalletManagement;
using Microsoft.AspNetCore.Mvc;

namespace BlockChainP2P.Wallet.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWallet _wallet;
    private readonly IP2PCaller _pcaller;
    public WalletController(
        IWallet wallet,
        IP2PCaller pcaller)
    {
        _wallet = wallet;
        _pcaller = pcaller;
    }

    /// <summary>
    /// Gets balance for running wallet instance
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(double), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance()
    {
        var result = await _wallet.GetBalance();
        return Ok(result);
    }

    /// <summary>
    /// Set node address
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    [HttpPost("callerAddress")]
    public async Task<IActionResult> GetBalance(string address)
    {
        _pcaller.SetNodeAddress(address);
        return Ok();
    }


    /// <summary>
    /// Gets wallet current public address
    /// </summary>
    /// <returns></returns>
    [HttpGet("address")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAddress()
    {
        var publicAddress = _wallet.GetActivePublicAddress();
        return Ok(publicAddress);
    }

    [HttpPost("setKey")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetKey(string privateKey) //example private Key: 51de9696926f38d48f58ed6017b3e31faaa9bf3125453c6d1311aabace37c7f8
    {
        var publicAddress = _wallet.SetKeyFromPrivateKey(privateKey);
        return Ok(publicAddress);
    }

    /// <summary>
    /// Creates transaction and broadcasts it to the network
    /// </summary>
    /// <returns></returns>
    [HttpPost("createTransaction")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTransaction([FromBody] TransactionRequest request)
    {
        try
        {
            if (request == null || !MasterValidator.IsValidAddress(request.Address) || request.Amount <= 0)
            {
                return BadRequest(new { message = "Invalid transaction data" });
            }

            var transaction = _wallet.CreateTransaction(request.Address, request.Amount);

            return Ok(new { message = "Transaction successfully created and broadcasted", transactionId = transaction.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Endpoint to get transaction details by transaction ID :TODO optional
    // Endpoint to generate a new private-public key pair :TODO optional
}
