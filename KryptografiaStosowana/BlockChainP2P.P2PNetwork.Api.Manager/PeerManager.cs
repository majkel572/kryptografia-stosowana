using BlockChainP2P.P2PNetwork.Api.Lib;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using System.Text.Json;
using System.Text;
using Serilog;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Request;

namespace BlockChainP2P.P2PNetwork.Api.Manager;

internal class PeerManager : IPeerManager
{
    private readonly IPeerData _peerData;

    public PeerManager(IPeerData peerData)
    {
        _peerData = peerData 
            ?? throw new ArgumentNullException(nameof(peerData));
    }

    public async Task<bool> ConnectWithPeerNetworkAsync(PeerLib peerToSendConnection)
    {
        await _peerData.AddPeerToKnownPeersAsync(peerToSendConnection);
        await _peerData.AddPeerToWorkingPeersAsync(peerToSendConnection);
        HttpClientHandler handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };

        using var httpClient = new HttpClient(handler);

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var currentNodeInfo = await _peerData.GetThisPeerInfoAsync();

        var payload = new RegisterAndBroadcastNewPeerRequest
        {
            AlreadyInformedPeers = new List<PeerLib>(),
            PeerToRegisterAndBroadcast = currentNodeInfo,
        };

        var json = JsonSerializer.Serialize(payload, options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://{peerToSendConnection.IPAddress}:{peerToSendConnection.Port}/PeerControler/RegisterAndBroadcastNewPeerAsync";
        var response = await httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Log.Information($"Node {peerToSendConnection.IPAddress}:{peerToSendConnection.Port} successfully broadcasted my presence.");
            var peerList = JsonSerializer.Deserialize<List<PeerLib>>(responseContent, options)!;
            await _peerData.AddPeersToWorkingAndKnownPeersInBulkAsync(peerList);
            return true;
        }
        else
        {
            Log.Error($"Node {peerToSendConnection.IPAddress}:{peerToSendConnection.Port} failed to broadcasted my presence.");
            return false;
        }
    }

    public async Task<List<PeerLib>> RegisterAndBroadcastNewPeerAsync(PeerLib peerToRegisterAndBroadcast, List<PeerLib> alreadyInformedPeers)
    {
        var workingPeerList = await _peerData.GetAllWorkingPeersAsync();
        
        await _peerData.AddPeerToKnownPeersAsync(peerToRegisterAndBroadcast); // TODO; dodac blokade wrzucania dwa razy tego samego
        await _peerData.AddPeerToWorkingPeersAsync(peerToRegisterAndBroadcast);

        var workingPeerToSendList = workingPeerList.Except(alreadyInformedPeers).ToList();

        var totalInformedPeers = alreadyInformedPeers.Union(workingPeerList).ToList();
        var thisNode = await _peerData.GetThisPeerInfoAsync();
        totalInformedPeers.Add(thisNode);
        if(workingPeerToSendList.Count > 0)
        {
            await Parallel.ForEachAsync(workingPeerList, async (peer, cancellationToken) =>
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                using var httpClient = new HttpClient(handler);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var payload = new RegisterAndBroadcastNewPeerRequest
                {
                    AlreadyInformedPeers = totalInformedPeers,
                    PeerToRegisterAndBroadcast = peerToRegisterAndBroadcast,
                };

                var json = JsonSerializer.Serialize(payload, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://{peer.IPAddress}:{peer.Port}/PeerControler/RegisterAndBroadcastNewPeerAsync";

                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully informed node {peer.IPAddress}:{peer.Port} about {peerToRegisterAndBroadcast.IPAddress}:{peerToRegisterAndBroadcast.Port}");
                }
                else
                {
                    Console.WriteLine($"Failed to inform node {peer.IPAddress}:{peer.Port} about {peerToRegisterAndBroadcast.IPAddress}:{peerToRegisterAndBroadcast.Port}");
                }
            });
        }
        else
        {
            Console.WriteLine("All peers have been informed.");
        }

        return workingPeerList;
    }
}
