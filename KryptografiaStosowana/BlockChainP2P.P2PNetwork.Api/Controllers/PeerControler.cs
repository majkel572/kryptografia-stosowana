using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BlockChainP2P.P2PNetwork.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PeerController : ControllerBase
{
    private readonly IPeerManager _peerManager;

    public PeerController(IPeerManager peerManager)
    {
        _peerManager = peerManager;
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectToPeer([FromBody] PeerLib peer)
    {
        var result = await _peerManager.ConnectWithPeerNetworkAsync(peer);
        return result ? Ok() : BadRequest();
    }
}
