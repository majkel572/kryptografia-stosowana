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

namespace BlockChainP2P.P2PNetwork.Api.Manager;

internal class BlockChainManager : IBlockChainManager
{
    private static readonly short BLOCK_GENERATION_INTERVAL = 10; // seconds
    private static readonly short DIFFICULTY_ADJUSTMENT_INTERVAL = 10; // blocks
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
        var newBlock = FindBlock(nextIndex, latestBlock.Hash, nextTimestamp, blockData, GetDifficulty(blockChain, latestBlock));

        if (ValidateNewBlock(newBlock, latestBlock))
        {
            await _blockChainData.AddBlockToBlockChainAsync(newBlock);
            await BroadcastNewBlockAsync(newBlock);
        }

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
            if (ValidateNewBlock(newBlock, latestBlock))
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

        if (currentBlockChain.Count() == 0 ||(ValidateBlockChain(newBlockChain, genesisBlock) && newBlockChain.Count > currentBlockChain.Count()))
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

    #region BlockMining

    private BlockLib FindBlock(int index, string previousHash, DateTime timestamp, string data, int difficulty)
    {
        int nonce = 0;
        while (true)
        {
            string hash = CalculateHash(index, previousHash, timestamp, data, difficulty, nonce);
            if (HashMatchesDifficulty(hash, difficulty))
            {
                return new BlockLib(index, hash, previousHash, timestamp, data, difficulty, nonce); // remember to broadcast it
            }
            nonce++;
        }
    }

    private string CalculateHash(int index, string previousHash, DateTime timestamp, string data, int difficulty, int nonce)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            string rawData = $"{index}{previousHash}{timestamp}{data}{difficulty}{nonce}";

            byte[] bytes = Encoding.UTF8.GetBytes(rawData);

            byte[] hashBytes = sha256.ComputeHash(bytes);

            StringBuilder hashString = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                hashString.Append(b.ToString("x2"));
            }

            var hash = hashString.ToString();

            return hash;
        }
    }

    #endregion BlockMining

    #region DifficultyManagement
    private int GetDifficulty(IEnumerable<BlockLib> blockChain, BlockLib latestBlock)
    {
        if (latestBlock.Index % DIFFICULTY_ADJUSTMENT_INTERVAL == 0 && latestBlock.Index != 0)
        {
            return GetAdjustedDifficulty(blockChain, latestBlock);
        }
        else
        {
            return latestBlock.Difficulty;
        }
    }


    private int GetAdjustedDifficulty(IEnumerable<BlockLib> blockChain, BlockLib latestBlock)
    {
        var adjustmentBlock = blockChain.ElementAtOrDefault(blockChain.Count() - DIFFICULTY_ADJUSTMENT_INTERVAL);
        var expectedTime = BLOCK_GENERATION_INTERVAL * DIFFICULTY_ADJUSTMENT_INTERVAL;
        var actualTime = (latestBlock.Timestamp - adjustmentBlock.Timestamp).TotalSeconds;
        if (actualTime < expectedTime / 2)
        {
            return adjustmentBlock.Difficulty + 1;
        }
        else if (actualTime > expectedTime * 2)
        {
            return adjustmentBlock.Difficulty - 1;
        }
        else
        {
            return adjustmentBlock.Difficulty;
        }
    }
    #endregion DifficultyManagement

    #region Validators

    private bool ValidateNewBlock(BlockLib newBlock, BlockLib previousBlock)
    {
        if (!ValidateBlockStructure(newBlock))
        {
            Log.Error("Invalid structure.");
            return false;
        }
        if (previousBlock.Index + 1 != newBlock.Index)
        {
            Log.Error("Block invalid! Wrong indexing.");
            return false;
        }
        else if (previousBlock.Hash != newBlock.PreviousHash)
        {
            Log.Error("Block invalid! Wrong previous hash.");
            return false;
        }
        else if (!IsValidTimestamp(newBlock, previousBlock))
        {
            Log.Error("Block invalid! Wrong timestamp.");
            return false;
        }
        else if (!HasValidHash(newBlock))
        {
            Log.Error("Block invalid! Wrong hash.");
            return false;
        }
        return true;
    }

    private bool ValidateBlockChain(List<BlockLib> blockChain, BlockLib genesisBlock)
    {
        var alienGenesisBlock = blockChain.FirstOrDefault(x => x.Index == 0);

        if (JsonConvert.SerializeObject(alienGenesisBlock) != JsonConvert.SerializeObject(genesisBlock))
        {
            Log.Error("Oof alien genesis block is corrupted!");
            return false;
        }

        for (int i = 0; i < blockChain.Count; i++)
        {
            if (ValidateNewBlock(blockChain[i], blockChain[i - 1]))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasValidHash(BlockLib newBlock)
    {
        if (!HashMatchesBlockContent(newBlock))
        {
            Log.Error("Invalid hash, got: " + newBlock.Hash);
        }

        if (!HashMatchesDifficulty(newBlock.Hash, newBlock.Difficulty))
        {
            Log.Error("Block difficulty not satisfied. Expected: " + newBlock.Difficulty + "got: " + newBlock.Hash);
        }
        return true;
    }

    private bool HashMatchesBlockContent(BlockLib block)
    {
        var blockHash = CalculateHash(block.Index, block.PreviousHash, block.Timestamp, block.Data, block.Difficulty, block.Nonce);
        return blockHash == block.Hash;
    }

    private bool HashMatchesDifficulty(string hash, int difficulty)
    {
        string hashInBinary = HexToBinary(hash);
        string requiredPrefix = new string('0', difficulty);
        return hashInBinary.StartsWith(requiredPrefix);
    }

    private string HexToBinary(string hexString)
    {
        return string.Join(string.Empty, hexString.Select(
            c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
        ));
    }

    private bool ValidateBlockStructure(BlockLib newBlock)
    {
        return newBlock.Hash is string &&
               newBlock.PreviousHash is string &&
               newBlock.Data is string;
    }

    private bool IsValidTimestamp(BlockLib newBlock, BlockLib previousBlock)
    {
        return (previousBlock.Timestamp.AddSeconds(-60) < newBlock.Timestamp) &&
            newBlock.Timestamp.AddSeconds(-60) < DateTime.Now;
    }
    #endregion Validators

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
            hash: CalculateHash(0, "previous hash", DateTime.Now, "Genesis Block", 1, 0),
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
