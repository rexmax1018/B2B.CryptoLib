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
    /// 離線 AES 金鑰產生器，產生金鑰與 IV 並序列化儲存為 JSON。
    /// </summary>
    /// <remarks>
    /// 此產生器固定產生 256-bit AES 金鑰與由平台產生的 IV。JSON 檔案包含原始
    /// 金鑰／IV，因此輸出目錄必須視為秘密儲存區；產生器僅供離線金鑰作業
    /// 使用，不應部署到 WebAPI。
    /// </remarks>
    public class AesKeyGenerator : IKeyGenerator<SymmetricKeyModel>
    {
        /// <summary>在記憶體中產生新的 256-bit AES 金鑰與 IV。</summary>
        /// <returns>包含新金鑰與 IV 的 <see cref="SymmetricKeyModel"/>。</returns>
        /// <remarks>不寫檔；回傳物件含秘密材料，呼叫端負責保護其生命週期。</remarks>
        public SymmetricKeyModel GenerateKeyOnly()
        {
            using (var aes = Aes.Create())
            {
                // 保留既有的 256-bit 材料並產生新的 IV；舊版 CBC 呼叫端
                // 依賴此模型形狀，即使 GCM v2 會在加密時自行產生 nonce（隨機數）。
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();

                return new SymmetricKeyModel { Key = aes.Key, IV = aes.IV };
            }
        }

        /// <summary>產生 AES 金鑰／IV 並以縮排 JSON 寫入 AES 設定目錄。</summary>
        /// <param name="filePath">可選檔名或路徑；實作只使用其檔名，省略時產生八碼隨機 <c>.json</c> 名稱。</param>
        /// <returns>描述輸出檔名與完整路徑的 <see cref="KeyGenerationResult"/>。</returns>
        /// <exception cref="System.InvalidOperationException"><see cref="B2B.CryptoLib.Config.CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="System.IO.IOException">輸出目錄或檔案無法建立或寫入。</exception>
        /// <remarks>此方法不會發布到執行階段的 <c>current</c>；輸出仍需受控搬運與權限設定。</remarks>
        public KeyGenerationResult GenerateAndSaveKey(string? filePath = null)
        {
            var model = GenerateKeyOnly();
            var fileName = Path.GetFileName(filePath ?? ConfigRoot.GenerateKeyFileName(".json"));
            var path = ConfigRoot.GetKeyPath("AES", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            File.WriteAllText(path, JsonConvert.SerializeObject(model, Formatting.Indented));

            return KeyGenerationResult.Create(CryptoAlgorithmType.AES, fileName, path);
        }
    }
}
