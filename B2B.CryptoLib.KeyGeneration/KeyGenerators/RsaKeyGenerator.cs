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
    /// <remarks>
    /// modulus size 取自 <see cref="CryptoConfig.Current"/> 的 RSA 設定。產生的 PEM
    /// writer 格式是 runtime .pub/.priv 與 legacy .pem loader 的相容性邊界；私鑰檔案
    /// 必須離線保存且不可提交到版本控制。
    /// </remarks>
    public class RsaKeyGenerator : IKeyGenerator<RsaKeyModel>
    {
        /// <summary>依目前 RSA 設定產生一組新的 RSA key pair。</summary>
        /// <returns>含 public/private PEM、modulus size 與 UTC 建立時間的 <see cref="RsaKeyModel"/>。</returns>
        /// <exception cref="InvalidOperationException"><see cref="CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="ArgumentException">RSA key size 不符合 Bouncy Castle generator 要求。</exception>
        public RsaKeyModel GenerateKeyOnly()
        {
            var size = CryptoConfig.Current.RSA.KeySize;
            var generator = new RsaKeyPairGenerator();

            // The configured modulus size is part of the generated-key contract;
            // do not silently substitute a library default during modernization.
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

        /// <summary>產生 RSA key pair 並以縮排 JSON 寫入 RSA 設定目錄。</summary>
        /// <param name="filePath">可選檔名或路徑；實作只使用其檔名，省略時產生八碼隨機 <c>.json</c> 名稱。</param>
        /// <returns>描述輸出檔名與完整路徑的 <see cref="KeyGenerationResult"/>。</returns>
        /// <exception cref="InvalidOperationException"><see cref="CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="IOException">輸出目錄或檔案無法建立或寫入。</exception>
        /// <remarks>此方法不發布到 runtime 的 <c>current</c>；輸出含 private key，必須受控保存。</remarks>
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
            // Keep the established PEM writer so generated RSA key files remain
            // compatible with existing .pub/.priv and legacy .pem consumers.
            using (var writer = new StringWriter())
            {
                new PemWriter(writer).WriteObject(value);

                return writer.ToString();
            }
        }
    }
}
