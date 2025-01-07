using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
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
    private readonly IBlockChainManager _bManager;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private Timer _timer;
    private readonly TimeSpan _poolMonitoringInterval = TimeSpan.FromSeconds(100);
    private readonly object _lock = new object();
    private bool _isMining = false;

    public Miner(
        ITransactionPool tpool,
        IBlockChainManager bmanager)
    {
        _transactionPool = tpool 
            ?? throw new ArgumentNullException(nameof(tpool));
        _timer = new Timer(MiningCallback, null, Timeout.InfiniteTimeSpan, _poolMonitoringInterval);
        _bManager = bmanager;
    }

    public void StartMining()
    {
        Console.WriteLine("Starting mining...");
        _timer.Change(TimeSpan.Zero, _poolMonitoringInterval);
    }

    public void StopMining()
    {
        Console.WriteLine("Stopping mining...");
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private async void MiningCallback(object state)
    {
        if (_isMining)
        {
            return;
        }

        var txs = await _transactionPool.GetTransactions();

        if(txs.Count > 0)
        {
            Console.WriteLine($"Mining job executing at: {DateTime.Now}");

            _isMining = true;
            try
            {
                var result = await _bManager.GenerateNextBlockWithTransaction();
                if(result is null)
                {
                    Console.WriteLine("Mining stopped, new block was added to blockchain while new was being mined.");
                }
                else
                {
                    Console.WriteLine("Successfully mined and broadcasted new block.");
                }
            }
            finally
            {
                _isMining = false;
                Console.WriteLine("Mining job completed.");
            }
        }
    }
}
