using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;

public interface IUnspentTransactionOutData
{
    void UpdateUnspentTransactionOutputs(List<TransactionLib> newTransactions);
    List<UnspentTransactionOutput> GetUnspentTxOut();
}
