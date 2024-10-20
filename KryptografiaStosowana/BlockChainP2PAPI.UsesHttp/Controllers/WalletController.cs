using BlockChainP2P.Lib.Interfaces.Models;
using BlockChainP2P.Lib.Models;
using Microsoft.AspNetCore.Mvc;

namespace BlockChainP2PAPI.UsesHttp.Controllers;

[ApiController]
[Route("[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWallet _wallet;

    public WalletController(Wallet wallet)
    {
        _wallet = wallet;
    }

    [HttpPost("generate-key")]
    public IActionResult GenerateKeyPair()
    {
        _wallet.GenerateNewKeyPair();
        return Ok("New key pair generated.");
    }

    [HttpGet("public-keys")]
    public IActionResult GetPublicKeys()
    {
        var keys = _wallet.GetPublicKeys();
        return Ok(keys);
    }

    [HttpPost("sign")]
    public IActionResult SignTransaction([FromBody] SignRequest request)
    {
        var signature = _wallet.SignTransaction(request.Message, request.KeyIndex);
        return Ok(new { Signature = signature });
    }

    [HttpPost("verify")]
    public IActionResult VerifySignature([FromBody] VerifyRequest request)
    {
        var isValid = _wallet.VerifySignature(request.Message, request.Signature, request.PublicKey);
        return Ok(new { IsValid = isValid });
    }
}
public class SignRequest
{
    public string Message { get; set; }
    public int KeyIndex { get; set; }
}

public class VerifyRequest
{
    public string Message { get; set; }
    public string Signature { get; set; }
    public string PublicKey { get; set; }
}
