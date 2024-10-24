using BlockChainP2P.P2PNetwork.Api.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;

public interface IPeerManager
{
    Task<List<PeerLib>> RegisterAndBroadcastNewPeerAsync(PeerLib peerToRegisterAndBroadcast, List<PeerLib> alreadyInformedPeers);
    Task<bool> ConnectWithPeerNetworkAsync(PeerLib peerToSendConnection);
    //Task<bool> AddPeerToKnownPeersAsync(PeerLib peer);
    //Task<bool> AddPeerToWorkingPeersAsync(PeerLib peer);
}
