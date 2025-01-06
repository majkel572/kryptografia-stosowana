using System;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Manager.Validators;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Transactions;

public class TransactionPoolBroadcastManager : ITransactionPoolBroadcastManager
{
  private readonly ITransactionPool _transactionPool;
  private readonly IUnspentTransactionOutData _unspentTransactionOutData;
  private readonly IPeerManager _peerManager;
  public TransactionPoolBroadcastManager(ITransactionPool transactions, IPeerManager peerManager, IUnspentTransactionOutData unspentTransactionOutData)
  {
    _transactionPool = transactions;
    _peerManager = peerManager;
    _unspentTransactionOutData = unspentTransactionOutData;
  }

  public async Task<List<TransactionLib>> GetAllTransactions() {
    return await _transactionPool.GetTransactions();
  }

  public async Task RequestAndUpdateTxPoolAsync(HubConnection connection)
  {
    connection.On<List<TransactionLib>>("ReceiveTxPool", async (txPool) =>
    {
      await ReceiveTransactions(txPool);
      Log.Information("Otrzymano i zaktualizowano tx pool od peera");
    });

    await connection.InvokeAsync("RequestTxPool");
  }

  public async Task<bool> ReceiveTransactions(List<TransactionLib> transactions) {
    var unspentTxOuts = _unspentTransactionOutData.GetUnspentTxOut();
    var currentTxPool = await _transactionPool.GetTransactions();
    var newTransactions = transactions.Where(tx => MasterValidator.ValidateTransaction(tx, unspentTxOuts)).ToList();
    newTransactions = newTransactions.Where(tx => MasterValidator.IsValidTxForPool(tx, currentTxPool)).ToList();
    
    if(newTransactions.Count > 0) {
      _transactionPool.AddTransactionsToMemPool(newTransactions);
      await _peerManager.BroadcastToPeers("ReceiveTxPool",newTransactions);
      return true;
    }
    return false;
  }

  public async Task AddNewTxPoolAsync(List<TransactionLib> transactions) {
    if(transactions.Count > 0) {
      _transactionPool.AddTransactionsToMemPool(transactions);
      await _peerManager.BroadcastToPeers("ReceiveTxPool",transactions);
    }
  }

}
