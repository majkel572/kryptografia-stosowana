using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Manager.Validators;
using BlockChainP2P.WalletHandler.KeyManagement;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Transactions;

public static class TransactionProcessor
{
    public static readonly double COINBASE_AMOUNT = 25.0;

    public static string GetTransactionId(TransactionLib transaction)
    {
        string txInContent = string.Concat(
            transaction.TransactionInputs.Select(txIn => txIn.TransactionOutputId + txIn.TransactionOutputIndex.ToString())
        );

        string txOutContent = string.Concat(
            transaction.TransactionOutputs.Select(txOut => txOut.Address + txOut.Amount.ToString())
        );

        string combinedContent = txInContent + txOutContent;

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedContent));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public static TransactionLib GetCoinbaseTransaction(string address, int blockIndex)
    {
        var tx = new TransactionLib();
        var txIn = new TransactionInputLib
        {
            Signature = "",
            TransactionOutputId = "",
            TransactionOutputIndex = blockIndex
        };

        tx.TransactionInputs = new List<TransactionInputLib> { txIn };
        tx.TransactionOutputs = new List<TransactionOutputLib> { new TransactionOutputLib(address, COINBASE_AMOUNT) };
        tx.Id = GetTransactionId(tx);
        return tx;
    }

    public static List<UnspentTransactionOutput> ProcessTransactions(List<TransactionLib> transactions, List<UnspentTransactionOutput> unspentTxOuts, int blockIndex)
    {
        if (!MasterValidator.IsValidTransactionsStructure(transactions))
        {
            return null;
        }

        if (!MasterValidator.ValidateBlockTransactions(transactions, unspentTxOuts, blockIndex))
        {
            Console.WriteLine("invalid block transactions");
            return null;
        }
        return new();
        //return UpdateUnspentTxOuts(transactions, unspentTxOuts);
    }

    public static string SignTransactionInput(
        TransactionLib transaction,
        int transactionInputIndex,
        string privateKeyHex,
        List<UnspentTransactionOutput> unspentTxOuts)
    {
        var txInput = transaction.TransactionInputs[transactionInputIndex];
        var dataToSign = transaction.Id;
        var referencedUnspentTxOut = unspentTxOuts.FirstOrDefault(x => x.TransactionOutputId == txInput.TransactionOutputId && x.TransactionOutputIndex == txInput.TransactionOutputIndex);
        if (referencedUnspentTxOut == default)
        {
            throw new ArgumentNullException("Referenced unspent transaction output not found.");
        }
        var referencedAddress = referencedUnspentTxOut.Address;

        var publicKey = KeyGenerator.GetPublicKeyBTC(privateKeyHex);
        if (publicKey != referencedAddress)
        {
            Console.WriteLine("trying to sign an input with private key that does not match the address that is referenced in txIn");
            throw new Exception("Private key does not match the referenced address.");
        }

        Key key = new Key(Encoders.Hex.DecodeData(privateKeyHex));

        byte[] dataToSignBytes;
        try
        {
            dataToSignBytes = Encoders.Hex.DecodeData(dataToSign);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to decode transaction ID from hex: " + ex.Message);
            throw new Exception("Invalid transaction ID format.");
        }

        if (dataToSignBytes.Length != 32)
        {
            Console.WriteLine("Invalid transaction ID length. Expected 32 bytes for SHA256 hash.");
            throw new Exception("Invalid transaction ID length.");
        }

        var hash = new uint256(dataToSignBytes);

        var signature = key.Sign(hash).ToDER();

        var signatureHex = BitConverter.ToString(signature).Replace("-", "").ToLower();

        return signatureHex;
    }

    public static (List<UnspentTransactionOutput> IncludedUnspentTxOuts, double LeftOverAmount) FindTxOutsForAmount(
        double amount,
        List<UnspentTransactionOutput> walletUnspentTxOuts)
    {
        double currentAmount = 0.0;
        var includedUnspentTxOuts = new List<UnspentTransactionOutput>();

        foreach (var myUnspentTxOut in walletUnspentTxOuts)
        {
            includedUnspentTxOuts.Add(myUnspentTxOut);
            currentAmount += myUnspentTxOut.Amount;

            if (currentAmount >= amount)
            {
                double leftOverAmount = currentAmount - amount;
                return (includedUnspentTxOuts, leftOverAmount);
            }
        }

        throw new InvalidOperationException("Not enough coins to send transaction");
    }

    private static TransactionInputLib ToUnsignedTxIn(UnspentTransactionOutput unspentTxOut)
    {
        return new TransactionInputLib
        {
            TransactionOutputId = unspentTxOut.TransactionOutputId,
            TransactionOutputIndex = unspentTxOut.TransactionOutputIndex
        };
    }

    public static void CreateUnsignedTxIns(
        double amount,
        List<UnspentTransactionOutput> myUnspentTxOuts,
        out List<TransactionInputLib> unsignedTxIns,
        out double leftOverAmount)
    {
        var result = FindTxOutsForAmount(amount, myUnspentTxOuts);
        unsignedTxIns = result.IncludedUnspentTxOuts.Select(ToUnsignedTxIn).ToList();
        leftOverAmount = result.LeftOverAmount;
    }

    public static List<TransactionOutputLib> CreateTxOuts(
        string receiverAddress,
        string myAddress,
        double amount,
        double leftOverAmount)
    {
        var txOut1 = new TransactionOutputLib(receiverAddress, amount);
        if (leftOverAmount == 0)
        {
            return new List<TransactionOutputLib> { txOut1 };
        }
        else
        {
            var leftOverTx = new TransactionOutputLib(myAddress, leftOverAmount);
            return new List<TransactionOutputLib> { txOut1, leftOverTx };
        }
    }

    public static TransactionLib CreateTransaction(
        string receiverAddress,
        double amount,
        string privateKey,
        List<UnspentTransactionOutput> unspentTxOuts,
        List<TransactionLib>? txPool) // TODO: transaction pool
    {
        Console.WriteLine("txPool: " + JsonConvert.SerializeObject(txPool));
        string myAddress = KeyGenerator.GetPublicKeyBTC(privateKey);
        var myUnspentTxOuts = unspentTxOuts.Where(uTxO => uTxO.Address == myAddress).ToList();

        //var myUnspentTxOuts = FilterTxPoolTxs(myUnspentTxOuts, txPool); // TODO: FilterTxPoolTxs

        var result = FindTxOutsForAmount(amount, myUnspentTxOuts);
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
            TransactionOutputs = CreateTxOuts(receiverAddress, myAddress, amount, leftOverAmount)
        };

        tx.Id = GetTransactionId(tx);

        tx.TransactionInputs = tx.TransactionInputs.Select((txIn, index) =>
        {
            txIn.Signature = SignTransactionInput(tx, index, privateKey, unspentTxOuts);
            return txIn;
        }).ToList();

        return tx;
    }
}
