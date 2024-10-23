using BlockChainP2P.P2PNetwork.Api.Lib;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using System.Text.Json;
using System.Text;
using Serilog;

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
        using var httpClient = new HttpClient();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var currentNodeInfo = await _peerData.GetThisPeerInfoAsync();

        var json = JsonSerializer.Serialize(currentNodeInfo, options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://{peerToSendConnection.IPAddress}:{peerToSendConnection.Port}/api/RegisterAndBroadcastNewPeerAsync";

        var response = await httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Log.Information($"Node {peerToSendConnection.IPAddress}:{peerToSendConnection.Port} successfully broadcasted my presence.");
            return true;
        }
        else
        {
            Log.Error($"Node {peerToSendConnection.IPAddress}:{peerToSendConnection.Port} failed to broadcasted my presence.");
            return false;
        }
    }

    public async Task<bool> RegisterAndBroadcastNewPeerAsync(PeerLib peerToRegisterAndBroadcast, List<PeerLib> alreadyInformedPeers)
    {
        await _peerData.AddPeerToKnownPeersAsync(peerToRegisterAndBroadcast); // TODO; dodac blokade wrzucania dwa razy tego samego
        await _peerData.AddPeerToWorkingPeersAsync(peerToRegisterAndBroadcast);

        var workingPeerList = await _peerData.GetAllWorkingPeersAsync();
        

        

        return true;
    }

    private async Task<bool> BroadcastNewPeerAsync(PeerLib newPeer, List<PeerLib> notifiedPeerList)
    {
        foreach(var existingPeer in notifiedPeerList)
        {
            // post request to api of each node
        }
        return true;
    }


}
