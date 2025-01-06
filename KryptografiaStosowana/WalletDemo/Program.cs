using BlockChainP2P.P2PNetwork.Api.Lib.KeyGen;
using BlockChainP2P.WalletHandler.KeyManagement;
using BlockChainP2P.WalletHandler.WalletManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace WalletDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Wallet wallet = new Wallet();
            KeyStorage keyStorage = new KeyStorage("SampleEncryptionKey1234");

            while (true)
            {
                Console.WriteLine("\nMenu:");
                Console.WriteLine("1. Create new key pair and add to wallet");
                Console.WriteLine("2. List all public keys");
                Console.WriteLine("3. Set active key pair");
                Console.WriteLine("4. Show active public address");
                Console.WriteLine("5. Sign transaction");
                Console.WriteLine("6. Save keys to file");
                Console.WriteLine("7. Load keys from file");
                Console.WriteLine("0. Exit");
                Console.Write("Choose an option: ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        CreateNewKeyPair(wallet);
                        break;
                    case "2":
                        ListPublicKeys(wallet);
                        break;
                    case "3":
                        SetActiveKeyPair(wallet);
                        break;
                    case "4":
                        ShowActivePublicAddress(wallet);
                        break;
                    case "5":
                        SignTransaction(wallet);
                        break;
                    case "6":
                        SaveKeysToFile(wallet, keyStorage);
                        break;
                    case "7":
                        LoadKeysFromFile(wallet, keyStorage);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        private static void CreateNewKeyPair(Wallet wallet)
        {
            var keyPair = KeyGenerator.GenerateKeys();
            wallet.AddKeyPair(keyPair);
            Console.WriteLine("New key pair created and added to wallet.");
        }

        private static void ListPublicKeys(Wallet wallet)
        {
            var publicKeys = wallet.GetPublicAddresses();
            if (publicKeys.Count == 0)
            {
                Console.WriteLine("No public keys found.");
            }
            else
            {
                Console.WriteLine("Public keys:");
                for (int i = 0; i < publicKeys.Count; i++)
                {
                    Console.WriteLine($"{i}: {publicKeys[i]}");
                }
            }
        }

        private static void SetActiveKeyPair(Wallet wallet)
        {
            Console.Write("Enter the index of the key pair to set as active: ");
            if (int.TryParse(Console.ReadLine(), out int index))
            {
                if (wallet.SetActiveKeyPair(index))
                {
                    Console.WriteLine("Active key pair updated.");
                }
                else
                {
                    Console.WriteLine("Invalid index.");
                }
            }
            else
            {
                Console.WriteLine("Invalid input.");
            }
        }

        private static void ShowActivePublicAddress(Wallet wallet)
        {
            Console.WriteLine("Active public address: " + wallet.GetActivePublicAddress());
        }

        private static void SignTransaction(Wallet wallet)
        {
            Console.Write("Enter the transaction data to sign: ");
            string transactionData = Console.ReadLine();

            try
            {
                //string signature = wallet.SignTransaction(transactionData);
                //Console.WriteLine("Transaction signed. Signature: " + signature);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private static void SaveKeysToFile(Wallet wallet, KeyStorage keyStorage)
        {
            Console.Write("Enter the file path to save keys: ");
            string filePath = Console.ReadLine();

            try
            {
                var keyPairs = wallet.GetKeyPairs();

                keyStorage.StorePrivateKeys(keyPairs, filePath);
                Console.WriteLine("Keys saved successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error saving keys: " + e.Message);
            }
        }

        private static void LoadKeysFromFile(Wallet wallet, KeyStorage keyStorage)
        {
            Console.Write("Enter the file path to load keys: ");
            string filePath = Console.ReadLine();

            try
            {
                var privateKeys = keyStorage.LoadPrivateKeys(filePath);
                foreach (var privateKeyHex in privateKeys)
                {
                    byte[] privateKeyBytes = Encoders.Hex.DecodeData(privateKeyHex);
                    Key privkey = new Key(privateKeyBytes);
                    PubKey publicKey = privkey.PubKey;
                    var keyPair = new KeyPairLib(publicKey, privkey);
                    wallet.AddKeyPair(keyPair);
                }

                Console.WriteLine("Keys loaded successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error loading keys: " + e.Message);
            }
        }
    }
}
