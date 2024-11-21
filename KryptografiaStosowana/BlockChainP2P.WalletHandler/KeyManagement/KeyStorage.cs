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
        byte[] salt = GenerateRandomBytes(16);
        using (var keyDerivationFunction = new Rfc2898DeriveBytes(_encryptionKey, salt, 10000, HashAlgorithmName.SHA256))
        {
            byte[] key = keyDerivationFunction.GetBytes(32);
            byte[] nonce = GenerateRandomBytes(12); // AesGcm używa nonce zamiast IV
            byte[] plaintext = Encoding.UTF8.GetBytes(privateKey);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16]; // Tag uwierzytelniający

            using (var newAes = new AesGcm(key, 16))
            {
                newAes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            // Łączymy wszystkie komponenty w jeden ciąg bajtów
            using (var ms = new MemoryStream())
            {
                ms.Write(salt, 0, salt.Length);
                ms.Write(nonce, 0, nonce.Length);
                ms.Write(tag, 0, tag.Length);
                ms.Write(ciphertext, 0, ciphertext.Length);

                return Convert.ToBase64String(ms.ToArray());
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

        using (var ms = new MemoryStream(fullCipher))
        {
            byte[] salt = new byte[16];
            ms.Read(salt, 0, salt.Length);

            byte[] nonce = new byte[12];
            ms.Read(nonce, 0, nonce.Length);

            byte[] tag = new byte[16];
            ms.Read(tag, 0, tag.Length);

            byte[] ciphertext = new byte[ms.Length - ms.Position];
            ms.Read(ciphertext, 0, ciphertext.Length);

            using (var keyDerivationFunction = new Rfc2898DeriveBytes(_encryptionKey, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] key = keyDerivationFunction.GetBytes(32);
                byte[] plaintext = new byte[ciphertext.Length];

                using (var aes = new AesGcm(key, 16))
                {
                    aes.Decrypt(nonce, ciphertext, tag, plaintext);
                    return Encoding.UTF8.GetString(plaintext);
                }
            }
        }
    }

}
