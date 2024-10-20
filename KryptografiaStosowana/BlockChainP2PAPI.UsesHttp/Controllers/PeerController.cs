using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using BlockChainP2P.Lib.Managers;
using BlockChainP2P.Lib.Interfaces.Managers;

namespace BlockChainP2PAPI.UsesHttp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PeerController : ControllerBase
    {
        private readonly IPeerManager _peerManager;
        private readonly HttpClient _httpClient;

        public PeerController(PeerManager peerManager, IHttpClientFactory httpClientFactory)
        {
            _peerManager = peerManager;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterPeer([FromBody] string peerUrl)
        {
            await _peerManager.RegisterPeer(peerUrl);
            return Ok($"Peer {peerUrl} added successfully.");
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastTransaction([FromBody] string transaction)
        {
            await _peerManager.BroadcastTransaction(transaction, _httpClient);
            return Ok("Transaction broadcasted.");
        }

        [HttpPost("receive")]
        public IActionResult ReceiveTransaction([FromBody] string transaction)
        {
            _peerManager.ReceiveTransaction(transaction);
            return Ok("Transaction received successfully.");
        }

        [HttpGet("transactions")]
        public IActionResult GetTransactions()
        {
            var transactions = _peerManager.GetTransactions();
            return Ok(transactions);
        }

        [HttpGet("discover")]
        public IActionResult DiscoverPeers()
        {
            return Ok(_peerManager.GetPeers());
        }
    }
}
