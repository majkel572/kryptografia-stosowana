using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.Model;

public class TransactionLib
{
    public string Id { get; set; }
    public List<TransactionInputLib> TransactionInputs { get; set; }
    public List<TransactionOutputLib> TransactionOutputs { get; set; }
}
