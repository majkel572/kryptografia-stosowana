using System;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Transactions;

public class TransactionPoolBroadcastManager : ITransactionPoolBroadcastManager
{
  private readonly ITransactionPool _transactionPool;
  private readonly IPeerManager _peerManager;
  public TransactionPoolBroadcastManager(ITransactionPool transactions, IPeerManager peerManager)
  {
    _transactionPool = transactions;
    _peerManager = peerManager;
  }

  public async Task<List<TransactionLib>> GetAllTransactions() {
    return await _transactionPool.GetTransactions();
  }

  public async Task RequestAndUpdateTxPoolAsync(HubConnection connection)
  {
    connection.On<List<TransactionLib>>("ReceiveTxPool", async (txPool) =>
    {
      await AddNewTxAsync(txPool);
      Log.Information("Otrzymano i zaktualizowano tx pool od peera");
    });

    await connection.InvokeAsync("RequestTxPool");
  }

  public async Task AddNewTxAsync(List<TransactionLib> transactions) {
    var currentTxPool = await _transactionPool.GetTransactions();
    var newTransactions = transactions
        .Where(t => !currentTxPool.Any(ct => ct.Id == t.Id))
        .ToList();

    if(newTransactions.Count > 0) {
      _transactionPool.AddTransactionsToMemPool(newTransactions);
      await _peerManager.BroadcastToPeers("ReceiveTxPool",newTransactions);
    }
  }

}
