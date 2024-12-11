using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;

public interface IPeerManager
{
    Task<bool> ConnectWithPeerNetworkAsync(PeerLib peerToConnect);
    Task RegisterPeerAsync(PeerLib peer);
    Task RegisterPeersAsync(List<PeerLib> peers);
    Task RemovePeerAsync(string connectionId);
    Task BroadcastToPeers<T>(string method, T data);
    Task<List<PeerLib>> GetKnownPeersAsync();
    Task RequestConnectionWithPeerAsync(PeerLib peer);
}
