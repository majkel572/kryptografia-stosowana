using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.P2PNetwork.Api.Lib.Model;

public class BlockLib
{
    public int Index { get; set; }
    public string Hash { get; set; }
    public string PreviousHash { get; set; }
    public DateTime Timestamp { get; set; }
    public List<TransactionLib> Data { get; set; }
    public int Difficulty { get; set; }
    public int Nonce { get; set; }

    public BlockLib(
        int index,
        string hash,
        string previousHash,
        DateTime timestamp,
        List<TransactionLib> data,
        int difficulty,
        int nonce)
    {
        Index = index;
        Hash = hash;
        PreviousHash = previousHash;
        Timestamp = timestamp;
        Data = data;
        Difficulty = difficulty;
        Nonce = nonce;
    }
}
