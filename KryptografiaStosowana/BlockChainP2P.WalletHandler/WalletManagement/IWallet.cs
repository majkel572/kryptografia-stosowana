using BlockChainP2P.P2PNetwork.Api.Lib.KeyGen;
using BlockChainP2P.P2PNetwork.Api.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.WalletHandler.WalletManagement;

public interface IWallet
{
    public string GetActivePublicAddress();
    public string GetActivePrivate();
    public List<KeyPairLib> GetKeyPairs();
    public List<string> GetPublicAddresses();
    public bool SetActiveKeyPair(int index);
    public void RemoveKeyPair(KeyPairLib keyPair);
    public void AddKeyPair(KeyPairLib keyPair);
    public Task<TransactionLib> CreateTransaction(
        string receiverAddress,
        double amount);
    Task<double> GetBalance();
    string SetKeyFromPrivateKey(string privateKeyStr);
    void CreateNewKeyPair();
    List<String> ListPublicKeys();
}
