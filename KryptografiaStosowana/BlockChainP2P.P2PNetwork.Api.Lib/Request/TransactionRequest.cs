using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.Request;

public class TransactionRequest
{
    public string Address { get; set; }
    public double Amount { get; set; }
}
