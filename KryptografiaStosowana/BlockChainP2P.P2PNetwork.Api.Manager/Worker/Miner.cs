using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Worker;

public class Miner : IMiner
{
    private readonly ITransactionPool _transactionPool;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public Miner(ITransactionPool tpool)
    {
        _transactionPool = tpool 
            ?? throw new ArgumentNullException(nameof(tpool));
    }

    public async Task StartProcessAsync()
    {
        await Task.Run(() => MonitorListForItems(_cancellationTokenSource.Token));
    }

    private async Task MonitorListForItems(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var txs = await _transactionPool.GetTransactions();
            if (txs.Count > 0)
            {
                await ProcessItemsAsync();
            }

            await Task.Delay(10000, token); // 10 second delay
        }
    }

    private async Task ProcessItemsAsync()
    {

    }
}
