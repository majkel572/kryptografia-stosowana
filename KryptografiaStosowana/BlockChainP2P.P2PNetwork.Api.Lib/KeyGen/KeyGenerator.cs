﻿using NBitcoin;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.KeyGen;

public static class KeyGenerator
{
    public static KeyPairLib GenerateKeys()
    {
        Key privateKey = new Key();

        PubKey publicKey = privateKey.PubKey.Compress();

        return new KeyPairLib(publicKey, privateKey);
    }
    public static PubKey GeneratePublicKeyFromPrivateKey(Key privateKey)
    {
        return privateKey.PubKey.Compress();
    }

    public static string GetPublicKeyBTC(string privateKeyHex)
    {
        var key = new Key(Encoders.Hex.DecodeData(privateKeyHex));
        return key.PubKey.ToHex();
    }
}
