using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;

public interface IBlockChainManager
{
    Task<BlockLib> GenerateNextBlockAsync(List<TransactionLib> blockData);
    Task<BlockLib> GenerateNextBlockWithTransaction();
    void ReplaceBlockChain(List<BlockLib> newBlockChain);
    Task BroadcastNewBlockAsync(BlockLib newBlock);
    Task<bool> ReceiveNewBlockAsync(BlockLib newBlock, HubConnection connection);
    Task CreateGenesisBlockAsync();
    Task RequestAndUpdateBlockchainAsync(HubConnection connection);
    Task<List<UnspentTransactionOutput>> GetAvailableUnspentTxOuts();
}
