using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BlockChainP2P.P2PNetwork.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PeerControler : ControllerBase
{
    private readonly IPeerManager _peerManager;

    public PeerControler(IPeerManager peerManager)
    {
        _peerManager = peerManager;
    }

    /// <summary>
    /// Broadcasts new peer and registers it in node
    /// </summary>
    /// <param name="peer">Address of a peer to register and broadcast</param>
    /// <param name="alreadyInformedPeers">List of already informed peers</param>
    /// <returns></returns>
    [HttpPost("RegisterAndBroadcastNewPeerAsync")]
    [ProducesResponseType(typeof(List<PeerLib>), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterAndBroadcastNewPeerAsync(RegisterAndBroadcastNewPeerRequest request) // TODO; api token zeby nie moc wywolac tego manualnie
    {
        Log.Information($"Trying to broadcast new peer with ip address: {request.PeerToRegisterAndBroadcast.IPAddress} and port number: {request.PeerToRegisterAndBroadcast.Port}");
        string clientIpAddress;
        int clientPort;
        (clientIpAddress, clientPort) = GetRemoteIpAndPort();
        Log.Information($"Request came from {clientIpAddress}:{clientPort}");

        var result = await _peerManager.RegisterAndBroadcastNewPeerAsync(request.PeerToRegisterAndBroadcast, request.AlreadyInformedPeers);

        Log.Information($"Successfully broadcasted new peer with ip address: {request.PeerToRegisterAndBroadcast.IPAddress} and port number: {request.PeerToRegisterAndBroadcast.Port}");
        return Ok(result);
    }

    private (string, int) GetRemoteIpAndPort()
    {
        var clientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var clientPort = HttpContext.Connection.RemotePort;

        return (clientIpAddress, clientPort);
    }
}
