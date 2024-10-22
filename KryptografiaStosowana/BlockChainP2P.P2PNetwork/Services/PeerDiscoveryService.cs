using BlockChainP2P.P2PNetwork.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Services
{
    internal class PeerDiscoveryService : IPeerDiscoveryService
    {
        public void DiscoverPeers()
        {
            Console.WriteLine("Discovering peers...");
        }
    }
}
