using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.Lib.Interfaces.Managers;

public interface IPeerManager
{
    public Task RegisterPeer(string peerUrl);

    public Task BroadcastTransaction(string transaction, HttpClient httpClient);

    public void ReceiveTransaction(string transaction);

    public List<string> GetTransactions();

    public Task DiscoverPeers(string newPeerUrl);

    public Task<List<string>> GetPeers();
}
