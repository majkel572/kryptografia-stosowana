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
        var text = File.ReadAllText(filePath);
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
            byte[] salt = GenerateRandomBytes(16); 
            using (var keyDerivationFunction = new Rfc2898DeriveBytes(_encryptionKey, salt, 10000, HashAlgorithmName.SHA256))
            {
                aes.Key = keyDerivationFunction.GetBytes(32);
                aes.GenerateIV(); 

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(salt, 0, salt.Length); 
                    ms.Write(aes.IV, 0, aes.IV.Length); 
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter writer = new StreamWriter(cs))
                    {
                        writer.Write(privateKey);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }

    private byte[] GenerateRandomBytes(int length)
    {
        byte[] randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return randomBytes;
    }


    private string DecryptPrivateKey(string encryptedPrivateKey)
    {
        byte[] fullCipher = Convert.FromBase64String(encryptedPrivateKey);

        using (MemoryStream ms = new MemoryStream(fullCipher))
        {
            byte[] salt = new byte[16];
            ms.Read(salt, 0, salt.Length); 

            byte[] iv = new byte[16];
            ms.Read(iv, 0, iv.Length);

            using (Aes aes = Aes.Create())
            using (var keyDerivationFunction = new Rfc2898DeriveBytes(_encryptionKey, salt, 10000, HashAlgorithmName.SHA256))
            {
                aes.Key = keyDerivationFunction.GetBytes(32);
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }

}
