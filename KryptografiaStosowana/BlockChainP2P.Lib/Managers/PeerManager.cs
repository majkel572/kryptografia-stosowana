using BlockChainP2P.Lib.Interfaces.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.Lib.Managers;

public class PeerManager : IPeerManager
{
    private List<string> _peers = new();
    private List<string> _transactions = new();

    #region Public
    public async Task<List<string>> GetPeers() => _peers;

    public async Task RegisterPeer(string peerUrl)
    {
        if (!_peers.Contains(peerUrl))
        {
            _peers.Add(peerUrl);
            Console.WriteLine($"Peer {peerUrl} registered.");
        }

        await DiscoverPeers(peerUrl); 
    }

    public async Task BroadcastTransaction(string transaction, HttpClient httpClient)
    {
        _transactions.Add(transaction);

        foreach (var peer in _peers)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync($"{peer}/receive-transaction", transaction);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Transaction sent to {peer}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send transaction to {peer}: {ex.Message}");
            }
        }
    }

    public void ReceiveTransaction(string transaction)
    {
        if (!_transactions.Contains(transaction))
        {
            _transactions.Add(transaction);
            Console.WriteLine($"Transaction received: {transaction}");
        }
    }

    public List<string> GetTransactions() => _transactions;

    public async Task DiscoverPeers(string newPeerUrl)
    {
        int newPeerPort = GetPortFromPeerUrl(newPeerUrl);

        var nearestPeers = _peers
            .Select(peer => new
            {
                Url = peer,
                Port = GetPortFromPeerUrl(peer),
                Difference = Math.Abs(GetPortFromPeerUrl(peer) - newPeerPort)
            })
            .OrderBy(p => p.Difference)
            .Take(3)
            .ToList();

        foreach (var neighbor in nearestPeers)
        {
            await NotifyPeerOfNewPeer(neighbor.Url, newPeerUrl);

            var discoveredPeers = await QueryPeerForKnownPeers(neighbor.Url);
            foreach (var discoveredPeer in discoveredPeers)
            {
                if (!_peers.Contains(discoveredPeer))
                {
                    _peers.Add(discoveredPeer);
                    Console.WriteLine($"Discovered new peer: {discoveredPeer}");
                }
            }
        }
    }

    #endregion Public

    #region Private
    private async Task<List<string>> QueryPeerForKnownPeers(string peer)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{peer}/api/peer/discover");
            if (response.IsSuccessStatusCode)
            {
                var knownPeers = await response.Content.ReadFromJsonAsync<List<string>>();
                return knownPeers ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to query {peer} for known peers: {ex.Message}");
        }

        return new List<string>();
    }

    private async Task NotifyPeerOfNewPeer(string peer, string newPeerUrl)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsJsonAsync($"{peer}/api/peer/register", newPeerUrl);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Peer {peer} notified of new peer {newPeerUrl}.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to notify {peer} about {newPeerUrl}: {ex.Message}");
        }
    }

    private int GetPortFromPeerUrl(string peerUrl)
    {
        var uri = new Uri(peerUrl);
        return uri.Port;
    }
    #endregion Private
}
