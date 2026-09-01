using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Models;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib.KeyGeneration.Services
{
    /// <summary>
    /// 以同一個統一名稱產生 AES 與 RSA 金鑰；沿用舊版檔名，但內容採用新版格式。
    /// </summary>
    /// <remarks>
    /// 這個 service 只產生 runtime 需要的 RSA/AES key set，並將檔案寫入
    /// <c>CryptoConfig.Current.KeyDirectory\update</c>。它不會搬移到 <c>current</c>、
    /// 不會刪除既有 key set，也不會呼叫 runtime update；部署者必須另外執行明確發布。
    /// </remarks>
    public class KeySetGenerationService : IKeySetGenerationService
    {
        private static readonly Regex SafeUnifiedNameRegex = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        private readonly IKeyGenerationService _keyGenerationService;
        private readonly ICryptoService _cryptoService;

        /// <summary>建立使用指定單鍵 generator 與低階 crypto service 的 key-set service。</summary>
        /// <param name="keyGenerationService">產生 RSA 與 AES model 的離線 service。</param>
        /// <param name="cryptoService">以 RSA OAEP 包裝新 AES material 的 service。</param>
        /// <exception cref="ArgumentNullException">任一相依服務為 null。</exception>
        public KeySetGenerationService(IKeyGenerationService keyGenerationService, ICryptoService cryptoService)
        {
            _keyGenerationService = keyGenerationService ?? throw new ArgumentNullException(nameof(keyGenerationService));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        }

        /// <summary>
        /// 產生一組使用舊版檔名、但採新版 OAEP 與 Key:IV 內容的檔案到 KeyDirectory/update。
        /// 未指定名稱時，使用 GenerateKeyFileName 產生的八碼字串作為共同前綴。
        /// </summary>
        /// <param name="unifiedName">可選的 key-set 名稱；只接受英數字元、底線與連字號。</param>
        /// <returns>包含 <c>.der</c>、<c>.public.pem</c>、<c>.private.pem</c> 三個 update 路徑的結果。</returns>
        /// <exception cref="ArgumentException">名稱含有不允許字元。</exception>
        /// <exception cref="InvalidOperationException"><see cref="CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="IOException">任何目標檔案已存在，或暫存／正式檔案無法寫入。</exception>
        /// <remarks>
        /// 呼叫會產生新的 RSA/AES material，先寫入唯一暫存檔，再依 public、private、AES
        /// 順序移入正式名稱；AES 檔案最後出現是為了讓 runtime 將完整三檔視為可發現的 key set。
        /// 即使呼叫失敗，已產生的秘密檔案也可能留在 update，部署流程應檢查並安全清理。
        /// </remarks>
        public KeySetGenerationResult GenerateAndSave(string? unifiedName = null)
        {
            var name = NormalizeUnifiedName(unifiedName);
            var rsa = _keyGenerationService.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var aes = _keyGenerationService.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);
            var updatePath = Path.Combine(CryptoConfig.Current.KeyDirectory, "update");

            Directory.CreateDirectory(updatePath);

            var aesPath = Path.Combine(updatePath, name + ".der");
            var publicKeyPath = Path.Combine(updatePath, name + ".public.pem");
            var privateKeyPath = Path.Combine(updatePath, name + ".private.pem");

            EnsureTargetDoesNotExist(aesPath);
            EnsureTargetDoesNotExist(publicKeyPath);
            EnsureTargetDoesNotExist(privateKeyPath);

            // New key sets keep the established filenames for deployment tooling;
            // legacy content parsing remains isolated in KeyManager.
            var aesMaterial = Convert.ToBase64String(aes.Key) + ":" + Convert.ToBase64String(aes.IV);
            var encryptedAes = _cryptoService.Encrypt(Encoding.UTF8.GetBytes(aesMaterial), CryptoAlgorithmType.RSA, rsa);
            var transactionId = Guid.NewGuid().ToString("N");
            var temporaryPaths = new[]
            {
                aesPath + "." + transactionId + ".tmp",
                publicKeyPath + "." + transactionId + ".tmp",
                privateKeyPath + "." + transactionId + ".tmp"
            };

            try
            {
                File.WriteAllText(temporaryPaths[1], rsa.PublicKey, Encoding.UTF8);
                File.WriteAllText(temporaryPaths[2], rsa.PrivateKey, Encoding.UTF8);
                File.WriteAllBytes(temporaryPaths[0], encryptedAes);

                // Publish the AES file last: KeyManager uses it as the discovery
                // marker and must never observe a partial three-file set.
                File.Move(temporaryPaths[1], publicKeyPath);
                File.Move(temporaryPaths[2], privateKeyPath);
                File.Move(temporaryPaths[0], aesPath);
            }
            finally
            {
                foreach (var temporaryPath in temporaryPaths)
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
            }

            return new KeySetGenerationResult
            {
                UnifiedName = name,
                AesKeyPath = aesPath,
                PublicKeyPath = publicKeyPath,
                PrivateKeyPath = privateKeyPath,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static string NormalizeUnifiedName(string? unifiedName)
        {
            if (string.IsNullOrWhiteSpace(unifiedName))
                return Path.GetFileNameWithoutExtension(CryptoConfig.GenerateKeyFileName());

            var name = Path.GetFileNameWithoutExtension(unifiedName);

            if (!SafeUnifiedNameRegex.IsMatch(name))
                throw new ArgumentException("unifiedName 僅可包含英數字元、底線與連字號。", nameof(unifiedName));

            return name;
        }

        private static void EnsureTargetDoesNotExist(string path)
        {
            if (File.Exists(path))
                throw new IOException($"金鑰檔案已存在：{path}");
        }

    }
}
