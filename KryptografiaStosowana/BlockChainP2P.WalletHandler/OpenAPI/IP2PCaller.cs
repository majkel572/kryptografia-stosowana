using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.WalletHandler.OpenAPI;

public interface IP2PCaller
{
    Task<bool> PassTransactionToNode(TransactionLib transactionToPass);
    Task<List<UnspentTransactionOutput>> GetAvailableTxOuts();
    void SetNodeAddress(string address);
}
