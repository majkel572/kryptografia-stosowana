using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.WalletHandler.KeyManagement
{
    public class KeyPair
    {
        public string PublicKey { get; }
        public string PrivateKey { get; }

        public KeyPair(string publicKey, string privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }
    }
}
