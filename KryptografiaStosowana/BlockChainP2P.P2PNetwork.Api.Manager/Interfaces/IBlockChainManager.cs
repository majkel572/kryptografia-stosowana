using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Manager.Interfaces;

public interface IBlockChainManager
{
    Task<BlockLib> GenerateNextBlockAsync(string blockData);
    void ReplaceBlockChain(List<BlockLib> newBlockChain);
}
