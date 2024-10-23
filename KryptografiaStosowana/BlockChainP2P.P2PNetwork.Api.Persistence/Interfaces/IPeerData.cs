using BlockChainP2P.P2PNetwork.Api.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;

public interface IPeerData
{
    Task<PeerLib?> GetWorkingPeerAsync(int id);
    Task<PeerLib?> GetKnownPeerAsync(int id);
    Task<List<PeerLib>> GetAllKnownPeersAsync();
    Task<List<PeerLib>> GetAllWorkingPeersAsync();
    Task<bool> DeletePeerFromWorkingPeersAsync(string ipAddress, string port);
    Task<PeerLib> AddPeerToKnownPeersAsync(PeerLib peer);
    Task<PeerLib> AddPeerToWorkingPeersAsync(PeerLib peer);
    Task<PeerLib> GetThisPeerInfoAsync();
}
