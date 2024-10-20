using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.Lib.Interfaces.Models
{
    public interface IWallet
    {
        public void GenerateNewKeyPair();

        public Task<List<string>> GetPublicKeys();

        public Task<string> SignTransaction(string message, int keyIndex);

        public Task<bool> VerifySignature(string message, string signature, string publicKey);
    }
}
