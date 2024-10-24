using BlockChainP2P.P2PNetwork.Api.Lib;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BlockChainP2P.P2PNetwork.Api.Controllers
{
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
        /// Connects to existing peer network
        /// </summary>
        /// <param name="peer">Address of a peer which is already in network</param>
        /// <returns></returns>
        [HttpPost("ConnectWithPeerNetwork")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        public async Task<IActionResult> ConnectWithPeerNetwork(PeerLib peer)
        {
            Log.Information($"Trying to register new peer with ip address: {peer.IPAddress} and port number: {peer.Port}");
            string clientIpAddress;
            int clientPort;
            (clientIpAddress, clientPort) = GetRemoteIpAndPort();
            Log.Information($"Request came from {clientIpAddress}:{clientPort}");

            var result = await _peerManager.ConnectWithPeerNetworkAsync(peer);
            // TODO; take from result list of known peers from the first host and put it in this node as known hosts
            var resText = $"Successfully registered new peer with ip address: {peer.IPAddress} and port number: {peer.Port}";
            Log.Information(resText);
            return Ok(resText);
        }

        /// <summary>
        /// Connects to existing peer network
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
}
