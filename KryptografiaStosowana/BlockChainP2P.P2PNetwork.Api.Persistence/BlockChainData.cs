using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using BlockChainP2P.P2PNetwork.Api.Persistence.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Persistence;

internal class BlockChainData : IBlockChainData
{
    private List<BlockLib> _blockChain;
    private readonly object _blockChainLock = new object();

    public BlockChainData()
    {
        _blockChain = new List<BlockLib>();
    }

    public async Task<BlockLib> AddBlockToBlockChainAsync(BlockLib block)
    {
        lock (_blockChainLock)
        {
            _blockChain.Add(block);
        }
        return block;
    }

    public async Task<IEnumerable<BlockLib>> GetBlockChainAsync()
    {
        return _blockChain.AsReadOnly();
    }

    public async Task<BlockLib> GetHighestIndexBlockAsync()
    {
        lock (_blockChainLock)
        {
            return _blockChain.OrderByDescending(x => x.Index).FirstOrDefault()!;
        }
    }

    public async Task<BlockLib> GetGenesisBlockAsync()
    {
        lock (_blockChainLock)
        {
            return _blockChain.FirstOrDefault(x => x.Index == 0)!;
        }
    }

    public void SwapBlockChains(List<BlockLib> newBlockChain)
    {
        lock (_blockChainLock)
        {
            _blockChain = newBlockChain;
        }
    }
}
