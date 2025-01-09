using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;

namespace BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;

public interface ITransactionPool
{
  Task<List<TransactionLib>> GetTransactions();
  void AddTransactionToMemPool(TransactionLib transaction);
  void AddTransactionsToMemPool(List<TransactionLib> transactions);
  void RemoveTransactionFromMemPool(TransactionLib transaction);
  Task UpdateTransactionPool(List<UnspentTransactionOutput> unspentTxOuts);
}
