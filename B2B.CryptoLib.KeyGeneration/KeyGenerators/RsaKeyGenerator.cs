using System;
using System.IO;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Models;
using B2B.CryptoLib.Models;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace B2B.CryptoLib.KeyGeneration.KeyGenerators
{
    /// <summary>
    /// 離線 RSA 金鑰產生器，產生 PEM 格式的公鑰與私鑰。
    /// </summary>
    public class RsaKeyGenerator : IKeyGenerator<RsaKeyModel>
    {
        public RsaKeyModel GenerateKeyOnly()
        {
            var size = CryptoConfig.Current.RSA.KeySize;
            var generator = new RsaKeyPairGenerator();

            generator.Init(new KeyGenerationParameters(new SecureRandom(), size));

            var keyPair = generator.GenerateKeyPair();

            return new RsaKeyModel
            {
                PrivateKey = WritePem(keyPair.Private),
                PublicKey = WritePem(keyPair.Public),
                KeySize = size,
                CreatedAt = DateTime.UtcNow
            };
        }

        public KeyGenerationResult GenerateAndSaveKey(string? filePath = null)
        {
            var model = GenerateKeyOnly();
            var fileName = Path.GetFileName(filePath ?? CryptoConfig.GenerateKeyFileName(".json"));
            var path = CryptoConfig.GetKeyPath("RSA", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            File.WriteAllText(path, JsonConvert.SerializeObject(model, Formatting.Indented));

            return KeyGenerationResult.Create(CryptoAlgorithmType.RSA, fileName, path);
        }

        private static string WritePem(object value)
        {
            using (var writer = new StringWriter())
            {
                new PemWriter(writer).WriteObject(value);

                return writer.ToString();
            }
        }
    }
}
