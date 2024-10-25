using BlockChainP2P.WalletHandler.KeyManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.WalletHandler.WalletManagement;

public class Wallet
{
    private readonly List<KeyPair> _keyPairs;
    private KeyPair _activeKeyPair;
    private readonly object _lock = new object();

    public Wallet()
    {
        _keyPairs = new List<KeyPair>();
    }

    public void AddKeyPair(KeyPair keyPair)
    {
        lock (_lock)
        {
            _keyPairs.Add(keyPair);
            if (_activeKeyPair == null)
            {
                _activeKeyPair = keyPair;
            }
        }
    }

    public void RemoveKeyPair(KeyPair keyPair)
    {
        lock (_lock)
        {
            _keyPairs.Remove(keyPair);
            if (_activeKeyPair == keyPair && _keyPairs.Count > 0)
            {
                _activeKeyPair = _keyPairs[0];
            }
            else if (_keyPairs.Count == 0)
            {
                _activeKeyPair = null;
            }
        }
    }

    public bool SetActiveKeyPair(int index)
    {
        lock (_lock)
        {
            if (index >= 0 && index < _keyPairs.Count)
            {
                _activeKeyPair = _keyPairs[index];
                return true;
            }
            return false;
        }
    }

    public List<string> GetPublicAddresses()
    {
        lock (_lock)
        {
            List<string> publicAddresses = new List<string>();
            foreach (var keyPair in _keyPairs)
            {
                publicAddresses.Add(keyPair.PublicKey);
            }
            return publicAddresses;
        }
    }

    public List<KeyPair> GetKeyPairs()
    {
        lock (_lock)
        {
            List<KeyPair> privateAddresses = new List<KeyPair>(_keyPairs);
            return privateAddresses;
        }
    }

    public string GetActivePublicAddress()
    {
        lock (_lock)
        {
            return _activeKeyPair?.PublicKey ?? "No active key";
        }
    }

    public string SignTransaction(string transactionData)
    {
        lock (_lock)
        {
            if (_activeKeyPair == null)
            {
                throw new InvalidOperationException("No active key pair available to sign transaction.");
            }

            using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            {
                ecdsa.ImportECPrivateKey(Convert.FromBase64String(_activeKeyPair.PrivateKey), out _);

                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(transactionData));
                var signature = ecdsa.SignHash(hash);

                return Convert.ToBase64String(signature);
            }
        }
    }
}