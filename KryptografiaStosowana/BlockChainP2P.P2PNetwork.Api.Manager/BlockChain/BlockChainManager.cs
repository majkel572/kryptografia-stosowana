using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.SignalR.Client;
using System.Runtime.CompilerServices;
using System.Transactions;
using NBitcoin;
using BlockChainP2P.P2PNetwork.Api.Manager.Transactions;
using BlockChainP2P.WalletHandler.WalletManagement;
using BlockChainP2P.P2PNetwork.Api.Lib.Transactions;
using BlockChainP2P.P2PNetwork.Api.Lib.Validators;
using BlockChainP2P.P2PNetwork.Api.Lib.Block;

namespace BlockChainP2P.P2PNetwork.Api.Manager.BlockChain;

internal class BlockChainManager : IBlockChainManager
{
    private readonly object _blockchainLock = new object();
    private readonly IBlockChainData _blockChainData;
    private readonly IPeerManager _peerManager;
    private readonly IWallet _wallet;
    private readonly IUnspentTransactionOutData _unspentTransactionOutData;
    private const string GENESIS_ADDRESS = "0259579f805a14cb86276c167d5e8fc737cd7d640e4850c3c94ec79337ee1c53e2"; 
    private const double GENESIS_AMOUNT = 50.0; 
    private readonly ITransactionPool _transactionPool;

    private List<TransactionLib> GENESIS_TRANSACTION => GetGenesisTransaction();

    public BlockChainManager(
        IBlockChainData blockChainData,
        IPeerManager peerManager,
        IWallet wallet,
        IUnspentTransactionOutData unspentTransactionOutData,
        ITransactionPool transactionPool)
    {
        _blockChainData = blockChainData
            ?? throw new ArgumentNullException(nameof(blockChainData));
        _peerManager = peerManager
            ?? throw new ArgumentNullException(nameof(peerManager));
        _wallet = wallet
            ?? throw new ArgumentNullException(nameof(wallet));
        _unspentTransactionOutData = unspentTransactionOutData
            ?? throw new ArgumentNullException(nameof(unspentTransactionOutData));
        _transactionPool = transactionPool 
            ?? throw new ArgumentNullException(nameof(transactionPool));
    }

    public async Task<BlockLib> GenerateNextBlockAsync(List<TransactionLib> blockData) // what if 2 threads create new block at the same time with same ids and one will write first?
    {
        var latestBlock = await _blockChainData.GetHighestIndexBlockAsync();
        var blockChain = await _blockChainData.GetBlockChainAsync();
        var nextIndex = latestBlock.Index + 1;
        var nextTimestamp = DateTime.Now;
        var newBlock = await FindBlock(nextIndex, latestBlock.Hash, nextTimestamp, blockData, BlockOperations.GetDifficulty(blockChain, latestBlock));

        if (newBlock == null)
        {
            return null;
        }

        if (MasterValidator.ValidateNewBlock(newBlock, latestBlock))
        {
            await _blockChainData.AddBlockToBlockChainAsync(newBlock);
            await BroadcastNewBlockAsync(newBlock);
        }

        // Usunięcie wydanych outputów z niewydanych outputów i dodanie nowych niewydanych outputów z transakcji z tego bloku, musi być po wykopaniu i po walidacji transakcji i bloku
        _unspentTransactionOutData.UpdateUnspentTransactionOutputs(blockData);
        var newUnspentTxOuts = _unspentTransactionOutData.GetUnspentTxOut();
        await _transactionPool.UpdateTransactionPool(newUnspentTxOuts);


        return newBlock;
    }

    public async Task<BlockLib> FindBlock(int index, string previousHash, DateTime timestamp, List<TransactionLib> data, int difficulty)
    {
        var latestBlock = await _blockChainData.GetHighestIndexBlockAsync();
        int nonce = 0;
        while (true)
        {
            string hash = BlockOperations.CalculateHash(index, previousHash, timestamp, data, difficulty, nonce);
            if (MasterValidator.HashMatchesDifficulty(hash, difficulty))
            {
                return new BlockLib(index, hash, previousHash, timestamp, data, difficulty, nonce); // remember to broadcast it
            }
            nonce++;

            if (nonce % 20 == 0)
            {
                var newLatestBlock = await _blockChainData.GetHighestIndexBlockAsync();
                if(newLatestBlock.Index != latestBlock.Index)
                {
                    return null; // block changed, stopped mining
                }
            }
        }
    }

    public async Task<BlockLib> GenerateNextBlockWithTransaction()
    {
        var transactionPool = await _transactionPool.GetTransactions();

        // ponizej zakladamy ze na komputerze na ktorym znajduje sie node jest rowniez wallet jakis ktory tutaj moznaby podpiac zeby mu zaplacic za wykopanie tego bloczku
        // nie ma w zasadzie mechanizmu na ten moment gdzie stwierdzamy jaki % transakcji stanowi fee dla minera, nalezaloby to uwzglednic w txouts niezuzytych po dokonaniu
        // transakcji tak aby txout nadawcy (czyli juz txin bo ma referencje) = txout odbiorcy + txout minera
        var coinbaseTx = TransactionProcessor.GetCoinbaseTransaction(_wallet.GetActivePublicAddress(), (await _blockChainData.GetHighestIndexBlockAsync()).Index + 1);
        var blockData = new List<TransactionLib> { coinbaseTx };
        blockData.AddRange(transactionPool);
        return await GenerateNextBlockAsync(blockData);
    }

    public async Task BroadcastNewBlockAsync(BlockLib newBlock)
    {
        //TODO: wyniesc brodkast do jakiegos signal managera
        await _peerManager.BroadcastToPeers("ReceiveNewBlock", newBlock);
    }

    public async Task<bool> ReceiveNewBlockAsync(BlockLib newBlock, HubConnection connection)
    {
        Log.Information($"Otrzymano nowy blok o indeksie {newBlock.Index}");
        var latestBlock = await _blockChainData.GetHighestIndexBlockAsync();
        // lock (_blockchainLock) {
        //     var latestBlock = _blockChainData.GetHighestIndexBlockAsync().GetAwaiter().GetResult();
        //     var validationResult = ValidateNewBlock(newBlock, latestBlock);
        // }
        // TODO: Block Validation
        // Sprawdź, czy otrzymany blok jest następny w kolejności
        if (newBlock.Index == latestBlock.Index + 1)
        {
            if (MasterValidator.ValidateNewBlock(newBlock, latestBlock))
            {
                await _blockChainData.AddBlockToBlockChainAsync(newBlock);
                await BroadcastNewBlockAsync(newBlock);
                Log.Information($"Otrzymano i dodano nowy prawidłowy blok o indeksie {newBlock.Index}");
                return true;
            }
            else
            {
                Log.Error($"Otrzymany blok {newBlock.Index} jest nieprawidłowy");
                await RequestAndUpdateBlockchainAsync(connection);
                return true;
            }
        }
        // Jeśli otrzymany blok jest dalej w przyszłości, może brakować nam bloków
        else if (newBlock.Index > latestBlock.Index + 1)
        {
            Log.Information("Otrzymano blok z przyszłości - potrzebne zaktualizowanie łańcucha");
            await RequestAndUpdateBlockchainAsync(connection);
            return false;
        }
        else
        {
            Log.Information($"Otrzymano stary lub duplikat bloku o indeksie {newBlock.Index}");
            return true;
        }
    }

    public async void ReplaceBlockChain(List<BlockLib> newBlockChain)
    {
        var currentBlockChain = await _blockChainData.GetBlockChainAsync();
        var genesisBlock = await _blockChainData.GetGenesisBlockAsync();

        if (MasterValidator.ValidateBlockChain(newBlockChain, genesisBlock))
        {
            // validate crosover point
            int lastIndex;
            bool isAlienAccepted;
            MasterValidator.FindCrossoverAndCalculateLength(currentBlockChain.ToList(), newBlockChain, out lastIndex, out isAlienAccepted);
            if(isAlienAccepted)
            {
                Log.Error("Received blockchain is valid. Replacing current blockchain with received blockchain.");
                var demountedTransactions = DemountBlocks(lastIndex, currentBlockChain.ToList());
                _blockChainData.SwapBlockChainsAsync(newBlockChain);
                _unspentTransactionOutData.ResetUnspentTransactionOutputs(newBlockChain);

                var unspentTxouts = _unspentTransactionOutData.GetUnspentTxOut();
                var txsToRemove = new List<TransactionLib>();
                foreach(var tx in demountedTransactions)
                {
                    if(!MasterValidator.ValidateTransaction(tx, unspentTxouts))
                    {
                        txsToRemove.Add(tx);
                    }
                }
                demountedTransactions.Except(txsToRemove);

                _transactionPool.AddTransactionsToMemPool(demountedTransactions);
            }

            //BroadcastLatest();
        }
        else
        {
            Log.Error("Received blockchain invalid.");
        }
    }

    public async Task CreateGenesisBlockAsync()
    {
        var existingGenesis = await _blockChainData.GetGenesisBlockAsync();
        if (existingGenesis != null)
        {
            Log.Warning("Genesis block already exists");
            return;
        }
        Log.Information("Creating genesis block");
        var genesisBlock = new BlockLib(
            index: 0,
            hash: "0d6006614ae1b0cd572dec0e356131c060a289d14d273a4da9c119bdd8859374",
            previousHash: "previous hash",
            timestamp: Convert.ToDateTime("2024-12-12T00:02:21.2383261+01:00"),
            data: GENESIS_TRANSACTION,
            difficulty: 2,
            nonce: 0
        );

        await _blockChainData.AddBlockToBlockChainAsync(genesisBlock);
        Log.Information("Genesis block created and added to blockchain");
    }

    public async Task RequestAndUpdateBlockchainAsync(HubConnection connection)
    {
        connection.On<IEnumerable<BlockLib>>("ReceiveBlockchain", (blockchain) =>
        {
            ReplaceBlockChain(blockchain.ToList());
            Log.Information("Otrzymano i zaktualizowano blockchain od peera");
        });

        // await connection.StartAsync();
        await connection.InvokeAsync("RequestBlockchain");
    }

    private List<TransactionLib> GetGenesisTransaction()
    {
        var transactionOutput = new TransactionOutputLib(
            address: GENESIS_ADDRESS,
            amount: GENESIS_AMOUNT
        );

        var transactionOutput2 = new TransactionOutputLib(
            address: "039b268ff353b04723a07ddc3cc25bea5a8eecb1cee551f2e6e994b6ab2c450a80",
            amount: GENESIS_AMOUNT
        );

        var genesisTransaction = new TransactionLib
        {
            Id = Guid.NewGuid().ToString(),
            TransactionInputs = new List<TransactionInputLib>(),
            TransactionOutputs = new List<TransactionOutputLib> { transactionOutput, transactionOutput2 }
        };

        _unspentTransactionOutData.UpdateUnspentTransactionOutputs(new List<TransactionLib> { genesisTransaction });

        return new List<TransactionLib> { genesisTransaction };
    }

    private List<TransactionLib> DemountBlocks(int lastIndex, List<BlockLib> currentBlockChain)
    {
        currentBlockChain = currentBlockChain.OrderBy(x => x.Index).ToList();
        var leftoverBlockchain = new List<BlockLib>();

        foreach (var block in currentBlockChain)
        {
            if (block.Index > lastIndex)
            {
                leftoverBlockchain.Add(block);
            }
        }

        return leftoverBlockchain.SelectMany(x => x.Data).ToList();
    }

    public async Task<List<UnspentTransactionOutput>> GetAvailableUnspentTxOuts()
    {
        var transactionPool = await _transactionPool.GetTransactions();
        var unspentTransactionOuts = _unspentTransactionOutData.GetUnspentTxOut();
        var filteredTxOuts = TransactionProcessor.FilterTxPoolTxs(unspentTransactionOuts, transactionPool);
        return filteredTxOuts;
    }
}
