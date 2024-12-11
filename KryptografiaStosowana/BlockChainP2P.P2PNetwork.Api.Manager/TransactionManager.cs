using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
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
    public static string GetTransactionId(Transaction transaction)
    {
        string txInContent = string.Concat(
            transaction.TxIns.Select(txIn => txIn.TxOutId + txIn.TxOutIndex.ToString())
        );

        string txOutContent = string.Concat(
            transaction.TxOuts.Select(txOut => txOut.Address + txOut.Amount.ToString())
        );

        string combinedContent = txInContent + txOutContent;

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedContent));
            StringBuilder hashString = new StringBuilder();

            foreach (byte b in hashBytes)
            {
                hashString.Append(b.ToString("x2")); // Converts to hexadecimal
            }

            return hashString.ToString();
        }
    }

}
