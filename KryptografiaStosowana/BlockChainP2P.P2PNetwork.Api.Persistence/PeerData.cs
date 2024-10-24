using BlockChainP2P.P2PNetwork.Api.Lib;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BlockChainP2P.P2PNetwork.Api.Persistence;

internal class PeerData : IPeerData
{
    private List<PeerLib> _knownPeers;
    private List<PeerLib> _workingPeers;
    private PeerLib _thisPeerInfo;
    private readonly object _knownPeersLock = new object();
    private readonly object _workingPeersLock = new object();

    public PeerData(IConfiguration config)
    {
        _knownPeers = new List<PeerLib>();
        _workingPeers = new List<PeerLib>();
        _thisPeerInfo = new PeerLib
        {
            IPAddress = config.GetSection("NodeIpAddress").Value!,
            Port = config.GetSection("NodePort").Value!
        };
    }

    public async Task<bool> DeletePeerFromWorkingPeersAsync(string ipAddress, string port)
    {
        lock (_knownPeersLock)
        {
            var peerToDelete = _knownPeers.FirstOrDefault(x => x.Port == port && x.IPAddress == ipAddress);
            if (peerToDelete == null)
            {
                return true;
            }
            _knownPeers.Remove(peerToDelete);
        }
        return true;
    }

    public async Task<List<PeerLib>> GetAllKnownPeersAsync()
    {
        return _knownPeers;
    }

    public async Task<List<PeerLib>> GetAllWorkingPeersAsync()
    {
        return _workingPeers;
    }

    public async Task<PeerLib> AddPeerToKnownPeersAsync(PeerLib peer)
    {
        lock (_knownPeersLock)
        {
            _knownPeers.Add(peer);
        }
        return peer;
    }

    public async Task<PeerLib> AddPeerToWorkingPeersAsync(PeerLib peer)
    {
        lock (_workingPeersLock)
        {
            _workingPeers.Add(peer);
        }
        return peer;
    }

    public async Task<PeerLib> GetThisPeerInfoAsync()
    {
        return _thisPeerInfo;
    }

    public async Task<bool> AddPeersToWorkingAndKnownPeersInBulkAsync(List<PeerLib> peer)
    {
        if(peer != null && peer.Count > 0)
        {
            lock (_workingPeersLock)
            {
                _workingPeers.AddRange(peer);
            }
            lock (_knownPeersLock)
            {
                _knownPeers.AddRange(peer);
            }
        }

        return true;
    }
}
