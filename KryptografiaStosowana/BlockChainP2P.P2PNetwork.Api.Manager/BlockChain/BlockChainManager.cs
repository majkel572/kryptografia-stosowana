﻿using BlockChainP2P.P2PNetwork.Api.Lib.Model;
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
using BlockChainP2P.P2PNetwork.Api.Manager.Validators;

namespace BlockChainP2P.P2PNetwork.Api.Manager.BlockChain;

internal class BlockChainManager : IBlockChainManager
{
    private readonly object _blockchainLock = new object();
    private readonly IBlockChainData _blockChainData;
    private readonly IPeerManager _peerManager;

    public BlockChainManager(IBlockChainData blockChainData, IPeerManager peerManager)
    {
        _blockChainData = blockChainData
            ?? throw new ArgumentNullException(nameof(blockChainData));
        _peerManager = peerManager
            ?? throw new ArgumentNullException(nameof(peerManager));
    }

    public async Task<BlockLib> GenerateNextBlockAsync(string blockData) // what if 2 threads create new block at the same time with same ids and one will write first?
    {
        var latestBlock = await _blockChainData.GetHighestIndexBlockAsync();
        var blockChain = await _blockChainData.GetBlockChainAsync();
        var nextIndex = latestBlock.Index + 1;
        var nextTimestamp = DateTime.Now;
        var newBlock = await BlockOperations.FindBlock(nextIndex, latestBlock.Hash, nextTimestamp, blockData, BlockOperations.GetDifficulty(blockChain, latestBlock));

        if (MasterValidator.ValidateNewBlock(newBlock, latestBlock))
        {
            await _blockChainData.AddBlockToBlockChainAsync(newBlock);
            await BroadcastNewBlockAsync(newBlock);
        }

        // Usunięcie wydanych outputów z niewydanych outputów i dodanie nowych niewydanych outputów z transakcji z tego bloku, musi być po wykopaniu i po walidacji transakcji i bloku

        return newBlock;
    }

    public async Task BroadcastNewBlockAsync(BlockLib newBlock)
    {
        //TODO: wyniesc brodkast do jakiegos signal managera
        await _peerManager.BroadcastToPeers("ReceiveNewBlock", newBlock);
    }

    public async Task<bool> ReceiveNewBlockAsync(BlockLib newBlock)
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
                return true;
            }
        }
        // Jeśli otrzymany blok jest dalej w przyszłości, może brakować nam bloków
        else if (newBlock.Index > latestBlock.Index + 1)
        {
            Log.Information("Otrzymano blok z przyszłości - potrzebne zaktualizowanie łańcucha");
            return false;
            // await RequestAndUpdateBlockchainAsync();
            // TODO: Zaimplementować żądanie brakujących bloków
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

        if (currentBlockChain.Count() == 0 || MasterValidator.ValidateBlockChain(newBlockChain, genesisBlock) && newBlockChain.Count > currentBlockChain.Count())
        {
            Log.Error("Received blockchain is valid. Replacing current blockchain with received blockchain.");
            _blockChainData.SwapBlockChainsAsync(newBlockChain);
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
            hash: BlockOperations.CalculateHash(0, "previous hash", DateTime.Now, "Genesis Block", 1, 0),
            previousHash: "previous hash",
            timestamp: DateTime.Now,
            data: "Genesis Block",
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
}
