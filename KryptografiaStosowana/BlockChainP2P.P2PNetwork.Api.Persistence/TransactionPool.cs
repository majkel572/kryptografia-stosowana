using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BlockChainP2P.P2PNetwork.Api.Persistence;

internal static class TransactionPool
{
    private static List<Transaction> transactions = new List<Transaction>();

    public static void AddTransaction(Transaction tx)
    {
        transactions.Add(tx);
    }

    public static List<Transaction> GetTransactions()
    {
        List<Transaction> txs = new List<Transaction>(transactions);
        transactions.Clear();
        return txs;
    }
}
