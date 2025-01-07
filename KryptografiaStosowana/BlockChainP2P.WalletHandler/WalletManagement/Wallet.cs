using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using NBitcoin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BlockChainP2P.P2PNetwork.Api.Lib.KeyGen;
using BlockChainP2P.P2PNetwork.Api.Lib.Transactions;
using BlockChainP2P.WalletHandler.OpenAPI;

namespace BlockChainP2P.WalletHandler.WalletManagement;

public class Wallet : IWallet
{
    private readonly List<KeyPairLib> _keyPairs;
    private KeyPairLib _activeKeyPair;
    private readonly object _lock = new object();

    private readonly IP2PCaller _pcaller;

    public Wallet(IP2PCaller pcaller)
    {
        _pcaller = pcaller
            ?? throw new ArgumentNullException(nameof(pcaller));

        _keyPairs = new List<KeyPairLib>();
        var privateKey = new Key(); 
        var publicKey = privateKey.PubKey; 

        var newKeyPair = new KeyPairLib(publicKey, privateKey);
        _keyPairs.Add(newKeyPair);

        _activeKeyPair = newKeyPair;
    }

    public void AddKeyPair(KeyPairLib keyPair)
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

    public void RemoveKeyPair(KeyPairLib keyPair)
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
                publicAddresses.Add(keyPair.GetPublicKeyHex());
            }
            return publicAddresses;
        }
    }

    public List<KeyPairLib> GetKeyPairs()
    {
        lock (_lock)
        {
            List<KeyPairLib> privateAddresses = new List<KeyPairLib>(_keyPairs);
            return privateAddresses;
        }
    }

    public string GetActivePublicAddress()
    {
        lock (_lock)
        {
            return _activeKeyPair?.GetPublicKeyHex() ?? "No active key";
        }
    }

    public string GetActivePrivate()
    {
        lock (_lock)
        {
            return _activeKeyPair?.GetPrivateKeyHex() ?? "No active key";
        }
    }


    /// <summary>
    /// Creates a new transaction to send a specified amount to a receiver's address. 
    /// Selects unspent transaction outputs from the sender's address to cover the transaction amount 
    /// and signs the inputs to ensure validity.
    /// </summary>
    /// <param name="receiverAddress">Address of the transaction recipient</param>
    /// <param name="amount">Amount to send to the recipient</param>
    /// <param name="privateKey">Private key of the sender, used for signing the transaction inputs</param>
    /// <param name="unspentTxOuts">List of all available unspent transaction outputs</param>
    /// <param name="txPool">Optional transaction pool storing not yet processed transactions</param>
    /// <returns>
    /// Returns a signed transaction ready to be added to the blockchain
    /// </returns>
    public TransactionLib CreateTransaction(
        string receiverAddress,
        double amount,
        List<UnspentTransactionOutput> unspentTxOuts)
    {
        // if keys rotate here should be this rotation included also (maybe)
        string myAddress = _activeKeyPair.GetPublicKeyHex(); /*KeyGenerator.GetPublicKeyBTC(privateKey);*/
        var myUnspentTxOuts = unspentTxOuts.Where(uTxO => uTxO.Address == myAddress).ToList();

        var result = TransactionProcessor.FindTxOutsForAmount(amount, myUnspentTxOuts);
        var includedUnspentTxOuts = result.IncludedUnspentTxOuts;
        var leftOverAmount = result.LeftOverAmount;

        var unsignedTxIns = includedUnspentTxOuts.Select(uTxO => new TransactionInputLib
        {
            TransactionOutputId = uTxO.TransactionOutputId,
            TransactionOutputIndex = uTxO.TransactionOutputIndex
        }).ToList();

        var tx = new TransactionLib
        {
            TransactionInputs = unsignedTxIns,
            TransactionOutputs = TransactionProcessor.CreateTxOuts(receiverAddress, myAddress, amount, leftOverAmount)
        };

        tx.Id = TransactionProcessor.GetTransactionId(tx);

        tx.TransactionInputs = tx.TransactionInputs.Select((txIn, index) =>
        {
            txIn.Signature = TransactionProcessor.SignTransactionInput(tx, index, _activeKeyPair.GetPrivateKeyHex(), unspentTxOuts);
            return txIn;
        }).ToList();

        return tx;
    }

    public async Task<double> GetBalance()
    {
        var unspentTxOuts = await _pcaller.GetAvailableTxOuts();
        var balance = TransactionProcessor.GetBalance(_activeKeyPair.GetPublicKeyHex(), unspentTxOuts);
        return balance;
    }
}