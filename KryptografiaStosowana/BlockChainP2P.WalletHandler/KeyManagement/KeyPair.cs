using NBitcoin;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.WalletHandler.KeyManagement
{
    public class KeyPair
    {
        public PubKey PublicKey { get; }
        public Key PrivateKey { get; }

        public KeyPair(PubKey publicKey, Key privateKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public string GetPublicKeyHex()
        {
            return PublicKey.Compress().ToHex();
        }
        
        public string GetPrivateKeyHex()
        {
            return Encoders.Hex.EncodeData(PrivateKey.ToBytes());
        }
    }
}
