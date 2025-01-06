using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;
using BlockChainP2P.P2PNetwork.Api.Manager.Validators;
using BlockChainP2P.WalletHandler.KeyManagement;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Transactions;

public static class TransactionProcessor
{
    public static readonly double COINBASE_AMOUNT = 25.0;

    /// <summary>
    /// Generates a unique identifier (ID) for a transaction by hashing the combined content of its inputs and outputs.
    /// </summary>
    /// <param name="transaction">Transaction for which the ID is generated</param>
    /// <returns>
    /// A string representing the SHA-256 hash of the concatenated details of the transaction inputs and outputs
    /// </returns>
    public static string GetTransactionId(TransactionLib transaction)
    {
        string txInContent = string.Concat(
            transaction.TransactionInputs.Select(txIn => txIn.TransactionOutputId + txIn.TransactionOutputIndex.ToString())
        );

        string txOutContent = string.Concat(
            transaction.TransactionOutputs.Select(txOut => txOut.Address + txOut.Amount.ToString())
        );

        string combinedContent = txInContent + txOutContent;

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combinedContent));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    /// <summary>
    /// Creates a coinbase transaction for the specified recipient address and block index.
    /// A coinbase transaction is the first transaction in a block, granting the mining reward to the specified address.
    /// </summary>
    /// <param name="address">The address to receive the coinbase reward.</param>
    /// <param name="blockIndex">The index of the block, used as the input index in the transaction.</param>
    /// <returns>
    /// Returns a new coinbase transaction containing a single input and a single output with the specified reward amount.
    /// </returns>
    public static TransactionLib GetCoinbaseTransaction(string address, int blockIndex)
    {
        var tx = new TransactionLib();
        var txIn = new TransactionInputLib
        {
            Signature = "",
            TransactionOutputId = "",
            TransactionOutputIndex = blockIndex
        };

        tx.TransactionInputs = new List<TransactionInputLib> { txIn };
        tx.TransactionOutputs = new List<TransactionOutputLib> { new TransactionOutputLib(address, COINBASE_AMOUNT) };
        tx.Id = GetTransactionId(tx);
        return tx;
    }

    /// <summary>
    /// Validates a list of transactions within a block and updates the state of unspent transaction outputs.
    /// </summary>
    /// <param name="transactions">List of transactions to validate and process</param>
    /// <param name="unspentTxOuts">Current list of unspent transaction outputs</param>
    /// <param name="blockIndex">Index of block containing transactions</param>
    /// <returns>List of unspent txouts</returns>
    public static List<UnspentTransactionOutput> ProcessTransactions(List<TransactionLib> transactions, List<UnspentTransactionOutput> unspentTxOuts, int blockIndex)
    {
        if (!MasterValidator.IsValidTransactionsStructure(transactions))
        {
            return null;
        }

        if (!MasterValidator.ValidateBlockTransactions(transactions, unspentTxOuts, blockIndex))
        {
            Console.WriteLine("invalid block transactions");
            return null;
        }
        return new();
        //return UpdateUnspentTxOuts(transactions, unspentTxOuts);
    }

    /// <summary>
    /// Signing transaction input
    /// </summary>
    /// <param name="transaction">Transaction to sign</param>
    /// <param name="transactionInputIndex">Transaction input index</param>
    /// <param name="privateKeyHex">Private key to sign transaction with</param>
    /// <param name="unspentTxOuts">All unspent transaction outputs</param>
    /// <returns>Hexadecimaly encoded transaction signature</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="Exception"></exception>
    public static string SignTransactionInput(
        TransactionLib transaction,
        int transactionInputIndex,
        string privateKeyHex,
        List<UnspentTransactionOutput> unspentTxOuts)
    {
        var txInput = transaction.TransactionInputs[transactionInputIndex];
        var dataToSign = transaction.Id;
        var referencedUnspentTxOut = unspentTxOuts.FirstOrDefault(x => x.TransactionOutputId == txInput.TransactionOutputId && x.TransactionOutputIndex == txInput.TransactionOutputIndex);
        if (referencedUnspentTxOut == default)
        {
            throw new ArgumentNullException("Referenced unspent transaction output not found.");
        }
        var referencedAddress = referencedUnspentTxOut.Address;

        var publicKey = KeyGenerator.GetPublicKeyBTC(privateKeyHex);
        if (publicKey != referencedAddress)
        {
            Console.WriteLine("trying to sign an input with private key that does not match the address that is referenced in txIn");
            throw new Exception("Private key does not match the referenced address.");
        }

        Key key = new Key(Encoders.Hex.DecodeData(privateKeyHex));

        byte[] dataToSignBytes;
        try
        {
            dataToSignBytes = Encoders.Hex.DecodeData(dataToSign);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to decode transaction ID from hex: " + ex.Message);
            throw new Exception("Invalid transaction ID format.");
        }

        if (dataToSignBytes.Length != 32)
        {
            Console.WriteLine("Invalid transaction ID length. Expected 32 bytes for SHA256 hash.");
            throw new Exception("Invalid transaction ID length.");
        }

        var hash = new uint256(dataToSignBytes);

        var signature = key.Sign(hash).ToDER();

        var signatureHex = BitConverter.ToString(signature).Replace("-", "").ToLower();

        return signatureHex;
    }

    /// <summary>
    /// Get balance for existing wallet address
    /// </summary>
    /// <param name="address">Wallet address to check balance for</param>
    /// <param name="unspentTxOuts">All unspent transaction outs</param>
    /// <returns>Returns balance for specified wallet address</returns>
    public static double GetBalance(string address, List<UnspentTransactionOutput> unspentTxOuts)
    {
        return unspentTxOuts
            .Where(x => x.Address == address)
            .Sum(x => x.Amount);
    }

    /// <summary>
    /// Searching for unspent transaction outputs to cover the amount needed for new transaction
    /// </summary>
    /// <param name="amount">Amount of the new transaction</param>
    /// <param name="walletUnspentTxOuts">Existing list of unspent transaction outputs</param>
    /// <returns>Returns usable txouts with amount to send back to the sender</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static (List<UnspentTransactionOutput> IncludedUnspentTxOuts, double LeftOverAmount) FindTxOutsForAmount(
        double amount,
        List<UnspentTransactionOutput> walletUnspentTxOuts)
    {
        double currentAmount = 0.0;
        var includedUnspentTxOuts = new List<UnspentTransactionOutput>();

        foreach (var myUnspentTxOut in walletUnspentTxOuts)
        {
            includedUnspentTxOuts.Add(myUnspentTxOut);
            currentAmount += myUnspentTxOut.Amount;

            if (currentAmount >= amount)
            {
                double leftOverAmount = currentAmount - amount;
                return (includedUnspentTxOuts, leftOverAmount);
            }
        }

        throw new InvalidOperationException("Not enough coins to send transaction");
    }

    /// <summary>
    /// Converts an unspent transaction output into an unsigned transaction input
    /// </summary>
    /// <param name="unspentTxOut">UTXO to be converted</param>
    /// <returns>TxIn containing the transaction output ID and index from the provided UTXO, without a signature</returns>
    private static TransactionInputLib ToUnsignedTxIn(UnspentTransactionOutput unspentTxOut)
    {
        return new TransactionInputLib
        {
            TransactionOutputId = unspentTxOut.TransactionOutputId,
            TransactionOutputIndex = unspentTxOut.TransactionOutputIndex
        };
    }

    /// <summary>
    /// Creates unsigned transaction inputs for a specified transaction amount by selecting appropriate unspent transaction outputs
    /// </summary>
    /// <param name="amount">The required transaction amount</param>
    /// <param name="myUnspentTxOuts">A list of UTXOs owned by the sender</param>
    /// <param name="unsignedTxIns">Outputs a list of unsigned TxIns created from the selected UTXOs</param>
    /// <param name="leftOverAmount">Outputs the remaining balance after the transaction amount is covered</param>
    public static void CreateUnsignedTxIns(
        double amount,
        List<UnspentTransactionOutput> myUnspentTxOuts,
        out List<TransactionInputLib> unsignedTxIns,
        out double leftOverAmount)
    {
        var result = FindTxOutsForAmount(amount, myUnspentTxOuts);
        unsignedTxIns = result.IncludedUnspentTxOuts.Select(ToUnsignedTxIn).ToList();
        leftOverAmount = result.LeftOverAmount;
    }

    /// <summary>
    /// Creates transaction outputs for transferring a specified amount to a receiver, with an optional leftover amount returned to the sender
    /// </summary>
    /// <param name="receiverAddress">Address of the transaction's recipient</param>
    /// <param name="myAddress">Sender's address for returning the leftover amount, if any</param>
    /// <param name="amount">Amount to transfer to the recipient</param>
    /// <param name="leftOverAmount">Remaining balance to be returned to the sender, if greater than zero</param>
    /// <returns>A list of TxOuts containing the recipient's and (if applicable) the sender's leftover balance</returns>
    public static List<TransactionOutputLib> CreateTxOuts(
        string receiverAddress,
        string myAddress,
        double amount,
        double leftOverAmount)
    {
        var txOut1 = new TransactionOutputLib(receiverAddress, amount);
        if (leftOverAmount == 0)
        {
            return new List<TransactionOutputLib> { txOut1 };
        }
        else
        {
            var leftOverTx = new TransactionOutputLib(myAddress, leftOverAmount);
            return new List<TransactionOutputLib> { txOut1, leftOverTx };
        }
    }

    /// <summary>
    /// Prevents double spending
    /// </summary>
    /// <param name="unspentTxOuts">List of unspent txouts</param>
    /// <param name="transactionPool">List of existing transactions waiting to be mined</param>
    /// <returns></returns>
    public static List<UnspentTransactionOutput> FilterTxPoolTxs(List<UnspentTransactionOutput> unspentTxOuts, List<TransactionLib> transactionPool) 
    {
        var txIns = transactionPool
        .SelectMany(tx => tx.TransactionInputs)
        .ToList();

        var removable = new List<UnspentTransactionOutput>();

        foreach (var unspentTxOut in unspentTxOuts)
        {
            var txIn = txIns.FirstOrDefault(aTxIn =>
                aTxIn.TransactionOutputIndex == unspentTxOut.TransactionOutputIndex && aTxIn.TransactionOutputId == unspentTxOut.TransactionOutputId);

            if (txIn != null)
            {
                removable.Add(unspentTxOut);
            }
        }

        return unspentTxOuts.Except(removable).ToList();
    }
}
