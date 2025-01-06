using NBitcoin;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.KeyGen;

public class KeyPairLib
{
    private PubKey PublicKey;
    private Key PrivateKey;

    public KeyPairLib(PubKey publicKey, Key privateKey)
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
