using BlockChainP2P.Lib.Interfaces.Models;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BlockChainP2P.Lib.Models;

public class Wallet : IWallet
{
    private List<(string PublicKey, string PrivateKey)> _keys = new();

    public Wallet()
    {
        GenerateNewKeyPair();
    }

    public void GenerateNewKeyPair()
    {
        using var rsa = new RSACryptoServiceProvider(2048);
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

        _keys.Add((publicKey, privateKey));
    }

    public async Task<List<string>> GetPublicKeys() => _keys.ConvertAll(k => k.PublicKey);

    public async Task<string> SignTransaction(string message, int keyIndex)
    {
        var privateKey = Convert.FromBase64String(_keys[keyIndex].PrivateKey);
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(privateKey, out _);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var signature = rsa.SignData(messageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    public async Task<bool> VerifySignature(string message, string signature, string publicKey)
    {
        var pubKey = Convert.FromBase64String(publicKey);
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(pubKey, out _);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var signatureBytes = Convert.FromBase64String(signature);
        return rsa.VerifyData(messageBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}

