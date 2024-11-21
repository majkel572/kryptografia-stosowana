using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;

namespace BlockChainP2P.P2PNetwork.Api.Lib.Request;

public class RegisterAndBroadcastNewPeerRequest
{
    public PeerLib PeerToRegisterAndBroadcast { get; set; }
    public List<PeerLib> AlreadyInformedPeers { get; set; }
}
