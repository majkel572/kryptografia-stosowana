using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.Model;

public class UnspentTransactionOutput
{
    public readonly string TransactionOutputId;
    public readonly int TransactionOutputIndex;
    public readonly string Address;
    public readonly double Amount;

    public UnspentTransactionOutput(
        string transactionOutputId,
        int transactionOutputIndex,
        string address, 
        double amount)
    {
        TransactionOutputId = transactionOutputId;
        TransactionOutputIndex = transactionOutputIndex;
        Address = address;
        Amount = amount;
    }
}
