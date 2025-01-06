using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;
using BlockChainP2P.WalletHandler.WalletManagement;
using Microsoft.AspNetCore.Mvc;

namespace BlockChainP2P.Wallet.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWallet _wallet;

    public WalletController(
        IWallet wallet)
    {
        _wallet = wallet;
    }

    ///// <summary>
    ///// Gets balance for running wallet instance
    ///// </summary>
    ///// <param name="address"></param>
    ///// <returns></returns>
    //[HttpGet("balance")]
    //[ProducesResponseType(typeof(BlockLib), StatusCodes.Status201Created)]
    //public async Task<IActionResult> GetBalance()
    //{
    //    var result = await _wallet.(request);
    //    return Ok(result);
    //}

    //// Endpoint to get the wallet's public address (or generate a new one)
    //[HttpGet("address")]
    //public IActionResult GetAddress()
    //{
    //    try
    //    {
    //        var publicAddress = _wallet.GetPublicAddress();
    //        return Ok(new { address = publicAddress });
    //    }
    //    catch (Exception ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}

    //// Endpoint to create a transaction
    //[HttpPost("transaction")]
    //public async Task<IActionResult> CreateTransaction([FromBody] TransactionRequest request)
    //{
    //    try
    //    {
    //        // Validate request data
    //        if (request == null || string.IsNullOrWhiteSpace(request.RecipientAddress) || request.Amount <= 0)
    //        {
    //            return BadRequest(new { message = "Invalid transaction data" });
    //        }

    //        // Create the transaction
    //        var transaction = await _transactionService.CreateTransactionAsync(request.SenderPrivateKey, request.RecipientAddress, request.Amount);

    //        if (transaction == null)
    //        {
    //            return BadRequest(new { message = "Transaction creation failed" });
    //        }

    //        // Broadcast the transaction (using your blockchain node or endpoint)
    //        var result = await _transactionService.BroadcastTransactionAsync(transaction);

    //        return Ok(new { message = "Transaction successfully created and broadcasted", transactionId = result });
    //    }
    //    catch (Exception ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}

    //// Endpoint to get transaction details by transaction ID
    //[HttpGet("transaction/{transactionId}")]
    //public async Task<IActionResult> GetTransaction(string transactionId)
    //{
    //    try
    //    {
    //        var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
    //        if (transaction == null)
    //        {
    //            return NotFound(new { message = "Transaction not found" });
    //        }

    //        return Ok(transaction);
    //    }
    //    catch (Exception ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}

    //// Endpoint to generate a new private-public key pair
    //[HttpPost("keys")]
    //public IActionResult GenerateNewKeyPair()
    //{
    //    try
    //    {
    //        var keyPair = _wallet.GenerateNewKeyPair();
    //        return Ok(new { publicKey = keyPair.PublicKey, privateKey = keyPair.PrivateKey });
    //    }
    //    catch (Exception ex)
    //    {
    //        return BadRequest(new { message = ex.Message });
    //    }
    //}
}
