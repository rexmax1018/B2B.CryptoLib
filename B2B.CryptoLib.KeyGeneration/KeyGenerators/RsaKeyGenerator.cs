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
        /// 模數大小取自 <see cref="CryptoConfig.Current"/> 的 RSA 設定。產生的 PEM
        /// 寫入器格式是執行階段 .pub/.priv 與舊版 .pem 載入器的相容性邊界；私鑰檔案
    /// 必須離線保存且不可提交到版本控制。
    /// </remarks>
    public class RsaKeyGenerator : IKeyGenerator<RsaKeyModel>
    {
        /// <summary>依目前 RSA 設定產生一組新的 RSA 金鑰對。</summary>
        /// <returns>含公開／私密 PEM、模數大小與 UTC 建立時間的 <see cref="RsaKeyModel"/>。</returns>
        /// <exception cref="InvalidOperationException"><see cref="CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="ArgumentException">RSA 金鑰大小不符合 Bouncy Castle 產生器要求。</exception>
        public RsaKeyModel GenerateKeyOnly()
        {
            var size = CryptoConfig.Current.RSA.KeySize;
            var generator = new RsaKeyPairGenerator();

            // 設定的模數大小是產生金鑰契約的一部分；現代化時不要靜默改用
            // 程式庫預設值。
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

        /// <summary>產生 RSA 金鑰對並以縮排 JSON 寫入 RSA 設定目錄。</summary>
        /// <param name="filePath">可選檔名或路徑；實作只使用其檔名，省略時產生八碼隨機 <c>.json</c> 名稱。</param>
        /// <returns>描述輸出檔名與完整路徑的 <see cref="KeyGenerationResult"/>。</returns>
        /// <exception cref="InvalidOperationException"><see cref="CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="IOException">輸出目錄或檔案無法建立或寫入。</exception>
        /// <remarks>此方法不發布到執行階段的 <c>current</c>；輸出含私密金鑰，必須受控保存。</remarks>
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
            // 保留既有的 PEM 寫入器，讓產生的 RSA 金鑰檔案持續與既有 .pub/.priv
            // 及舊版 .pem 呼叫端相容。
            using (var writer = new StringWriter())
            {
                new PemWriter(writer).WriteObject(value);

                return writer.ToString();
            }
        }
    }
}
