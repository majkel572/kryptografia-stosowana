using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using System.Runtime.CompilerServices;

namespace BlockChainP2P.P2PNetwork.Api.Persistence;

internal class PeerData : IPeerData
{
    private Dictionary<string, HubConnection> _connections;
    private List<PeerLib> _knownPeers;
    private PeerLib _thisPeerInfo;
    private readonly object _knownPeersLock = new object();
    private readonly object _connectionsLock = new object();

    public PeerData(IConfiguration config)
    {
        _knownPeers = new List<PeerLib>();
        _thisPeerInfo = new PeerLib
        {
            IPAddress = config.GetSection("NodeIpAddress").Value!,
            Port = config.GetSection("NodePort").Value!
        };
        _connections = new Dictionary<string, HubConnection>();
    }

    public async Task<List<PeerLib>> GetAllKnownPeersAsync()
    {
        return new List<PeerLib>(_knownPeers);
    }

    public async Task<Dictionary<string, HubConnection>> GetAllConnectionsAsync()
    {
        return _connections;
    }

    public async Task<PeerLib> AddPeerToKnownPeersAsync(PeerLib peer) => await Task.FromResult(AddPeerToKnownPeers(peer));
    
    private PeerLib AddPeerToKnownPeers(PeerLib peer)
    {
        lock (_knownPeersLock)
        {
            if (!_knownPeers.Any(x => x.IPAddress == peer.IPAddress && x.Port == peer.Port))
            {
                _knownPeers.Add(peer);
            }
        }
        return peer;
    }

    public async Task AddHubConnection(string connectionKey, HubConnection connection)
    {
        lock (_connectionsLock)
        {
            _connections[connectionKey] = connection;
        }
    }

    public async Task<HubConnection?> GetHubConnection(string connectionKey)
    {
        lock (_connectionsLock)
        {
            if(_connections.ContainsKey(connectionKey)) 
            {
                return _connections[connectionKey];
            }
            return null;
        }
    }

    public async Task<HubConnection?> GetHubConnectionByConnectionId(string connectionId)
    {
        lock (_connectionsLock)
        {
            var conn = _connections.FirstOrDefault(x => x.Value.ConnectionId == connectionId);
            return conn.Equals(default(KeyValuePair<string, HubConnection>)) ? null : conn.Value;
        }
    }

    public async Task<bool> RemoveHubConnection(string connectionId)
    {
        lock (_knownPeersLock) {
            var peer = _knownPeers.FirstOrDefault(x => x.ConnectionId == connectionId);
            var tmp = peer.Equals(default(PeerLib)) ? true : _knownPeers.Remove(peer);
        }
        lock (_connectionsLock)
        {
            var conn = _connections.FirstOrDefault(x => x.Value.ConnectionId == connectionId);
            return conn.Equals(default(KeyValuePair<string, HubConnection>)) ? true : _connections.Remove(conn.Key);
        }
    }

    public async Task<bool> IsConnectedToPeer(string connectionKey)
    {
        lock (_connectionsLock)
        {
            return _connections.ContainsKey(connectionKey);
        }
    }

    public async Task<PeerLib> GetThisPeerInfoAsync()
    {
        return _thisPeerInfo;
    }

}
