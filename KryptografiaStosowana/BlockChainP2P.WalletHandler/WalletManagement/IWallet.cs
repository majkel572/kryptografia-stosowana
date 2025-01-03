using BlockChainP2P.WalletHandler.KeyManagement;
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
    public List<KeyPair> GetKeyPairs();
    public List<string> GetPublicAddresses();
    public bool SetActiveKeyPair(int index);
    public void RemoveKeyPair(KeyPair keyPair);
    public void AddKeyPair(KeyPair keyPair);
}
