using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib;

public class RegisterAndBroadcastNewPeerRequest
{
    public PeerLib PeerToRegisterAndBroadcast { get; set; }
    public List<PeerLib> AlreadyInformedPeers { get; set; }
}
