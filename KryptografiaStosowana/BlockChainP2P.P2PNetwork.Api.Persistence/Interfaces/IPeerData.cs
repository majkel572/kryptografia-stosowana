using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;

public interface IPeerData
{
    Task<List<PeerLib>> GetAllKnownPeersAsync();
    Task<List<PeerLib>> GetAllWorkingPeersAsync();
    Task<bool> DeletePeerFromWorkingPeersAsync(string ipAddress, string port);
    Task<PeerLib> AddPeerToKnownPeersAsync(PeerLib peer);
    Task<PeerLib> AddPeerToWorkingPeersAsync(PeerLib peer);
    Task<bool> AddPeersToWorkingAndKnownPeersInBulkAsync(List<PeerLib> peer);
    Task<PeerLib> GetThisPeerInfoAsync();
    Task<PeerLib?> GetPeerByConnectionIdAsync(string connectionId);
}
