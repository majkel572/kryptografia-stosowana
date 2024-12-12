using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.WalletHandler.KeyManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BlockChainP2P.P2PNetwork.Api.Manager;

internal class TransactionManager : ITransactionManager
{
    public string GetTransactionId(TransactionLib transaction)
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

    public string SignTransactionInput(
        TransactionLib transaction,
        int transactionInputIndex, 
        string privateKey, 
        List<UnspentTransactionOutput> unspentTxOuts)
    {
        var txInput = transaction.TransactionInputs[transactionInputIndex];
        var dataToSign = transaction.Id;
        var referencedUnspentTxOut = unspentTxOuts.FirstOrDefault(x => x.TransactionOutputId == txInput.TransactionOutputId && x.TransactionOutputIndex == txInput.TransactionOutputIndex);
        if(referencedUnspentTxOut == default)
        {
            throw new ArgumentNullException(nameof(referencedUnspentTxOut));
        }
        var referencedAddress = referencedUnspentTxOut.Address;
        var key = "a"; // TODO: generate key to sign transaction from private key

        if(key != referencedAddress)
        {
            throw new ArgumentException("Trying to sign an input with private key that does not match the address refered in transactionInput");
        }

        var signature = "a" ;// TODO: sign data to sign with 

        return signature;
    }

}
