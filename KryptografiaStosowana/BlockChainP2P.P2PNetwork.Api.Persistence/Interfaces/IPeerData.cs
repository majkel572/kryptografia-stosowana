using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;

public interface IPeerData
{
    Task<List<PeerLib>> GetAllKnownPeersAsync();    
    Task<Dictionary<string, HubConnection>> GetAllConnectionsAsync();
    Task<PeerLib> AddPeerToKnownPeersAsync(PeerLib peer);
    Task AddHubConnection(string connectionKey, HubConnection connection);
    Task<PeerLib> GetThisPeerInfoAsync();
    Task<bool> IsConnectedToPeer(string connectionKey);
    Task<HubConnection?> GetHubConnection(string connectionKey);
    Task<bool> RemoveHubConnection(string connectionId);
    Task<HubConnection?> GetHubConnectionByConnectionId(string connectionId);
    void ChangeIsBrodcasting();
    bool IsBroadcasting();
}
