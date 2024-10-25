using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.WalletHandler.KeyManagement
{
    public class KeyGenerator
    {
        public KeyPair GenerateKeys()
        {
            using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            {
                string privateKey = Convert.ToBase64String(ecdsa.ExportECPrivateKey());

                string publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

                return new KeyPair(publicKey, privateKey);
            }
        }
        public static string GeneratePublicKeyFromPrivateKey(string privateKey)
        {
            using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            {
                ecdsa.ImportECPrivateKey(Convert.FromBase64String(privateKey), out _);

                return Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
            }
        }
    }
}
