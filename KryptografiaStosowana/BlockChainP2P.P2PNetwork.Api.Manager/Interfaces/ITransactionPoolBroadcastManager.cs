using System;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;

public interface ITransactionPoolBroadcastManager
{
  Task RequestAndUpdateTxPoolAsync(HubConnection connection);
  Task AddNewTxPoolAsync(List<TransactionLib> transactions);
  Task<List<TransactionLib>> GetAllTransactions();
  Task<bool> ReceiveTransactions(List<TransactionLib> transactions);
}
