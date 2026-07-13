using System.IO;
using System.Security.Cryptography;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Models;
using B2B.CryptoLib.Models;
using Newtonsoft.Json;
using ConfigRoot = B2B.CryptoLib.Config.CryptoConfig;

namespace B2B.CryptoLib.KeyGeneration.KeyGenerators
{
    /// <summary>
    /// 離線 AES 金鑰產生器，產生 Key 與 IV 並序列化儲存為 JSON。
    /// </summary>
    public class AesKeyGenerator : IKeyGenerator<SymmetricKeyModel>
    {
        public SymmetricKeyModel GenerateKeyOnly()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();

                return new SymmetricKeyModel { Key = aes.Key, IV = aes.IV };
            }
        }

        public KeyGenerationResult GenerateAndSaveKey(string filePath = null)
        {
            var model = GenerateKeyOnly();
            var fileName = Path.GetFileName(filePath ?? ConfigRoot.GenerateKeyFileName(".json"));
            var path = ConfigRoot.GetKeyPath("AES", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllText(path, JsonConvert.SerializeObject(model, Formatting.Indented));

            return KeyGenerationResult.Create(CryptoAlgorithmType.AES, fileName, path);
        }
    }
}