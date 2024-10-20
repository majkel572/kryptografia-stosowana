using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChainP2P.WalletHandler.KeyManagement;

public class KeyStorage
{
    private readonly string _encryptionKey;

    public KeyStorage(string encryptionKey)
    {
        _encryptionKey = encryptionKey;
    }

    public void StorePrivateKeys(List<KeyPair> keyPairs, string filePath)
    {
        var encryptedKeys = new List<string>();
        foreach (var keyPair in keyPairs)
        {
            encryptedKeys.Add(EncryptPrivateKey(keyPair.PrivateKey));
        }
        File.WriteAllText(filePath, JsonConvert.SerializeObject(encryptedKeys));
    }

    public List<string> LoadPrivateKeys(string filePath)
    {
        var encryptedKeys = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(filePath));
        var privateKeys = new List<string>();

        foreach (var encryptedKey in encryptedKeys)
        {
            privateKeys.Add(DecryptPrivateKey(encryptedKey));
        }

        return privateKeys;
    }

    private string EncryptPrivateKey(string privateKey)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
            aes.IV = new byte[16];

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter writer = new StreamWriter(cs))
                {
                    writer.Write(privateKey);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    private string DecryptPrivateKey(string encryptedPrivateKey)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
            aes.IV = new byte[16];

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(encryptedPrivateKey)))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (StreamReader reader = new StreamReader(cs))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
