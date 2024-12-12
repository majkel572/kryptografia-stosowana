using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;

public interface ITransactionManager
{
    string GetTransactionId(TransactionLib transaction);
}
