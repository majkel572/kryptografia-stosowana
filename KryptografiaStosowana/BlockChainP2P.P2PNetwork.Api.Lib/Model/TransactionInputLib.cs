using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.Model;

public class TransactionInputLib
{
    public string TransactionOutputId { get; set; }
    public int TransactionOutputIndex { get; set; }
    public string Signature { get; set; }
}
