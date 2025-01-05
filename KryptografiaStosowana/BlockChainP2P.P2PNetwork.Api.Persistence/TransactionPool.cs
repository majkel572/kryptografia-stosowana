using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlockChainP2P.P2PNetwork.Api.Persistence;

internal class TransactionPool : ITransactionPool
{
    private List<TransactionLib> _transactions = new List<TransactionLib>();
    private readonly object _transactionsLock = new object();

    public async Task<List<TransactionLib>> GetTransactions()
    {
        List<TransactionLib> txs = new List<TransactionLib>(_transactions);
        return txs;
    }

    public void AddTransactionToMemPool(TransactionLib transaction) {
        lock (_transactionsLock) {
            _transactions.Add(transaction);
        }
    }

    public void AddTransactionsToMemPool(List<TransactionLib> transactions) {
        lock (_transactionsLock) {
            _transactions.AddRange(transactions);
        }
    }

    public void RemoveTransactionFromMemPool(TransactionLib transaction) {
        lock (_transactionsLock) {
            _transactions.Remove(transaction);
        }
    }
}
