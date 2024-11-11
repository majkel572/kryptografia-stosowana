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

namespace BlockChainP2P.P2PNetwork.Api.Manager;

internal class BlockChainManager : IBlockChainManager
{
    private static readonly short BLOCK_GENERATION_INTERVAL = 10; // seconds
    private static readonly short DIFFICULTY_ADJUSTMENT_INTERVAL = 10; // blocks

    private readonly IBlockChainData _blockChainData;

    public BlockChainManager(IBlockChainData blockChainData)
    {
        _blockChainData = blockChainData 
            ?? throw new ArgumentNullException(nameof(blockChainData));
    }

    public async Task<BlockLib> GenerateNextBlockAsync(string blockData) // what if 2 threads create new block at the same time with same ids and one will write first?
    {
        var latestBlock = await _blockChainData.GetHighestIndexBlockAsync();
        var blockChain = await _blockChainData.GetBlockChainAsync();
        var nextIndex = latestBlock.Index + 1;
        var nextTimestamp = DateTime.Now;
        var newBlock = FindBlock(nextIndex, latestBlock.PreviousHash, nextTimestamp, blockData, GetDifficulty(blockChain, latestBlock));

        if (ValidateNewBlock(newBlock, latestBlock))
        {
            await _blockChainData.AddBlockToBlockChainAsync(newBlock);
            //BroadcastNewBlock();
        }

        return newBlock;
    }

    public async void ReplaceBlockChain(List<BlockLib> newBlockChain)
    {
        var currentBlockChain = await _blockChainData.GetBlockChainAsync();
        var genesisBlock = await _blockChainData.GetGenesisBlockAsync();

        if (ValidateBlockChain(newBlockChain, genesisBlock) && newBlockChain.Count > currentBlockChain.Count())
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
        if (ValidateBlockStructure(newBlock))
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
}
