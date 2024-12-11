using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.Model;

public class TransactionOutputLib
{
    public string Address { get; set; }
    public double Amount { get; set; }

    public TransactionOutputLib(string address, double amount) 
    { 
        Address = address;
        Amount = amount;
    }
}
