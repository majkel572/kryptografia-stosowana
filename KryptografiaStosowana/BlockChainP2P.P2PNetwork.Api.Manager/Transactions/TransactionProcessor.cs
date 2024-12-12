using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.WalletHandler.KeyManagement;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
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
            StringBuilder hashString = new StringBuilder();

            foreach (byte b in hashBytes)
            {
                hashString.Append(b.ToString("x2"));
            }

            return hashString.ToString();
        }
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

}
