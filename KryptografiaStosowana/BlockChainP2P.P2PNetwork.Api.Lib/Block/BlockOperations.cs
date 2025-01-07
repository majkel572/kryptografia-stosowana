using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Lib.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.Block;

public static class BlockOperations
{
    private static readonly short BLOCK_GENERATION_INTERVAL = 10; // seconds
    private static readonly short DIFFICULTY_ADJUSTMENT_INTERVAL = 10; // blocks

    #region BlockMining
    public static string CalculateHash(int index, string previousHash, DateTime timestamp, List<TransactionLib> data, int difficulty, int nonce)
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
    public static int GetDifficulty(IEnumerable<BlockLib> blockChain, BlockLib latestBlock)
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


    public static int GetAdjustedDifficulty(IEnumerable<BlockLib> blockChain, BlockLib latestBlock)
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

}
