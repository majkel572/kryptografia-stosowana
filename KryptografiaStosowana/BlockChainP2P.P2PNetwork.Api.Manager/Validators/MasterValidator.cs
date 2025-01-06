using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.BlockChain;
using BlockChainP2P.P2PNetwork.Api.Manager.Transactions;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Validators;

public static class MasterValidator
{
    #region Blocks

    public static bool ValidateNewBlock(BlockLib newBlock, BlockLib previousBlock)
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

    public static bool ValidateBlockChain(List<BlockLib> blockChain, BlockLib genesisBlock)
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

    public static bool HasValidHash(BlockLib newBlock)
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

    public static bool HashMatchesBlockContent(BlockLib block)
    {
        var blockHash = BlockOperations.CalculateHash(block.Index, block.PreviousHash, block.Timestamp, block.Data, block.Difficulty, block.Nonce);
        return blockHash == block.Hash;
    }

    public static bool HashMatchesDifficulty(string hash, int difficulty)
    {
        string hashInBinary = HexToBinary(hash);
        string requiredPrefix = new string('0', difficulty);
        return hashInBinary.StartsWith(requiredPrefix);
    }

    public static string HexToBinary(string hexString)
    {
        return string.Join(string.Empty, hexString.Select(
            c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
        ));
    }

    public static bool ValidateBlockStructure(BlockLib newBlock)
    {
        return newBlock.Hash is string &&
               newBlock.PreviousHash is string &&
               newBlock.Data is string;
    }

    public static bool IsValidTimestamp(BlockLib newBlock, BlockLib previousBlock)
    {
        return previousBlock.Timestamp.AddSeconds(-60) < newBlock.Timestamp &&
            newBlock.Timestamp.AddSeconds(-60) < DateTime.Now;
    }
    #endregion Blocks

    #region Transactions
    public static bool ValidateTransaction(TransactionLib transaction, List<UnspentTransactionOutput> aUnspentTxOuts)
    {
        if (TransactionProcessor.GetTransactionId(transaction) != transaction.Id)
        {
            Console.WriteLine("invalid tx id: " + transaction.Id);
            return false;
        }

        bool hasValidTxIns = transaction.TransactionInputs.All(txIn => ValidateTxIn(txIn, transaction, aUnspentTxOuts));

        if (!hasValidTxIns)
        {
            Console.WriteLine("some of the txIns are invalid in tx: " + transaction.Id);
            return false;
        }

        double totalTxInValues = transaction.TransactionInputs.Sum(txIn => GetTxInAmount(txIn, aUnspentTxOuts));
        double totalTxOutValues = transaction.TransactionOutputs.Sum(txOut => txOut.Amount);

        if (totalTxOutValues != totalTxInValues)
        {
            Console.WriteLine("totalTxOutValues != totalTxInValues in transaction: " + transaction.Id);
            return false;
        }

        return true;
    }

    public static bool IsValidTransactionsStructure(List<TransactionLib> transactions)
    {
        return transactions.All(IsValidTransactionStructure);
    }

    public static bool IsValidTransactionStructure(TransactionLib transaction)
    {
        if (string.IsNullOrEmpty(transaction.Id))
        {
            Console.WriteLine("transactionId missing");
            return false;
        }

        if (transaction.TransactionInputs == null || !transaction.TransactionInputs.All(IsValidTxInStructure))
        {
            Console.WriteLine("invalid txIns structure in transaction");
            return false;
        }

        if (transaction.TransactionOutputs == null || !transaction.TransactionOutputs.All(IsValidTxOutStructure))
        {
            Console.WriteLine("invalid txOuts structure in transaction");
            return false;
        }

        return true;
    }

    public static bool IsValidTxInStructure(TransactionInputLib txIn)
    {
        if (txIn == null)
        {
            Console.WriteLine("txIn is null");
            return false;
        }
        if (string.IsNullOrEmpty(txIn.Signature))
        {
            Console.WriteLine("invalid signature type in txIn");
            return false;
        }
        if (string.IsNullOrEmpty(txIn.TransactionOutputId))
        {
            Console.WriteLine("invalid txOutId type in txIn");
            return false;
        }
        if (txIn.TransactionOutputIndex < 0)
        {
            Console.WriteLine("invalid txOutIndex type in txIn");
            return false;
        }
        return true;
    }

    public static bool IsValidTxOutStructure(TransactionOutputLib txOut)
    {
        if (txOut == null)
        {
            Console.WriteLine("txOut is null");
            return false;
        }
        if (string.IsNullOrEmpty(txOut.Address))
        {
            Console.WriteLine("invalid address type in txOut");
            return false;
        }
        if (!IsValidAddress(txOut.Address))
        {
            Console.WriteLine("invalid TxOut address");
            return false;
        }
        if (txOut.Amount < 0)
        {
            Console.WriteLine("invalid amount type in txOut");
            return false;
        }
        return true;
    }

    public static bool IsValidAddress(string address)
    {
        if (address.Length != 130)
        {
            Console.WriteLine("invalid public key length");
            return false;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(address, "^[a-fA-F0-9]+$"))
        {
            Console.WriteLine("public key must contain only hex characters");
            return false;
        }
        if (!(address.StartsWith("04") || address.StartsWith("02") || address.StartsWith("03")))
        {
            Console.WriteLine("public key must start with 04, 02 or 03");
            return false;
        }
        return true;
    }

    public static bool ValidateTxIn(TransactionInputLib txIn, TransactionLib transaction, List<UnspentTransactionOutput> unspentTxOuts)
    {
        var referencedUTxOut = unspentTxOuts.FirstOrDefault(uTxO => uTxO.TransactionOutputId == txIn.TransactionOutputId && uTxO.TransactionOutputIndex == txIn.TransactionOutputIndex);
        if (referencedUTxOut == null)
        {
            Console.WriteLine("referenced txOut not found: " + JsonConvert.SerializeObject(txIn));
            return false;
        }
        var address = referencedUTxOut.Address;

        PubKey pubKey;
        try
        {
            pubKey = new PubKey(address);
        }
        catch
        {
            Console.WriteLine("Invalid public key format: " + address);
            return false;
        }

        var dataToVerifyHex = transaction.Id;
        byte[] dataToVerifyBytes;
        try
        {
            dataToVerifyBytes = Encoders.Hex.DecodeData(dataToVerifyHex);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to decode transaction ID from hex: " + ex.Message);
            return false;
        }

        var hash = new uint256(dataToVerifyBytes);
        byte[] signatureBytes;
        try
        {
            signatureBytes = Encoders.Hex.DecodeData(txIn.Signature);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to decode signature from hex: " + ex.Message);
            return false;
        }

        ECDSASignature ecdsaSignature;
        try
        {
            ecdsaSignature = ECDSASignature.FromDER(signatureBytes);
        }
        catch
        {
            Console.WriteLine("Invalid DER format for signature: " + txIn.Signature);
            return false;
        }

        bool isValid = pubKey.Verify(hash, ecdsaSignature);
        if (!isValid)
        {
            Console.WriteLine("Signature verification failed for txIn: " + JsonConvert.SerializeObject(txIn));
        }

        return isValid;
    }

    public static double GetTxInAmount(TransactionInputLib txIn, List<UnspentTransactionOutput> unspentTxOuts)
    {
        var utxo = unspentTxOuts.FirstOrDefault(uTxO => uTxO.TransactionOutputId == txIn.TransactionOutputId && uTxO.TransactionOutputIndex == txIn.TransactionOutputIndex);
        return utxo?.Amount ?? 0.0;
    }

    public static bool HasDuplicates(List<TransactionInputLib> txIns)
    {
        var duplicateGroups = txIns
            .GroupBy(txIn => txIn.TransactionOutputId + txIn.TransactionOutputIndex)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            Console.WriteLine("duplicate txIn: " + group.Key);
        }

        return duplicateGroups.Any();
    }

    public static bool ValidateBlockTransactions(List<TransactionLib> transactions, List<UnspentTransactionOutput> unspentTxOuts, int blockIndex)
    {
        var coinbaseTx = transactions[0];
        if (!ValidateCoinbaseTx(coinbaseTx, blockIndex))
        {
            Console.WriteLine("invalid coinbase transaction: " + JsonConvert.SerializeObject(coinbaseTx));
            return false;
        }

        var txIns = transactions.SelectMany(tx => tx.TransactionInputs).ToList();

        if (HasDuplicates(txIns))
        {
            return false;
        }

        var normalTransactions = transactions.Skip(1).ToList();
        return normalTransactions.All(tx => ValidateTransaction(tx, unspentTxOuts));
    }

    public static bool IsValidTxForPool(TransactionLib tx, List<TransactionLib> transactionPool)
    {
        var txPoolIns = transactionPool.SelectMany(tx => tx.TransactionInputs).ToList();

        foreach (var txIn in tx.TransactionInputs)
        {
            if (txPoolIns.Any(txPoolIn => txIn.TransactionOutputIndex == txPoolIn.TransactionOutputIndex && txIn.TransactionOutputId == txPoolIn.TransactionOutputId))
            {
                Console.WriteLine("txIn already found in the txPool");
                return false;
            }
        }

        return true;
    }
    #endregion Transactions

    #region CoinbaseTransactions
    public static bool ValidateCoinbaseTx(TransactionLib transaction, int blockIndex)
    {
        if (transaction == null)
        {
            Console.WriteLine("the first transaction in the block must be coinbase transaction");
            return false;
        }
        if (TransactionProcessor.GetTransactionId(transaction) != transaction.Id)
        {
            Console.WriteLine("invalid coinbase tx id: " + transaction.Id);
            return false;
        }
        if (transaction.TransactionInputs.Count != 1)
        {
            Console.WriteLine("one txIn must be specified in the coinbase transaction");
            return false;
        }
        if (transaction.TransactionInputs[0].TransactionOutputIndex != blockIndex)
        {
            Console.WriteLine("the txIn signature in coinbase tx must be the block height");
            return false;
        }
        if (transaction.TransactionOutputs.Count != 1)
        {
            Console.WriteLine("invalid number of txOuts in coinbase transaction");
            return false;
        }
        if (transaction.TransactionOutputs[0].Amount != TransactionProcessor.COINBASE_AMOUNT)
        {
            Console.WriteLine("invalid coinbase amount in coinbase transaction");
            return false;
        }
        return true;
    }
    #endregion CoinbaseTransactions

}
