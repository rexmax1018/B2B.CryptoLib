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
    /// 這個服務只產生執行階段需要的 RSA/AES 金鑰組，並將檔案寫入
    /// <c>CryptoConfig.Current.KeyDirectory\update</c>。它不會搬移到 <c>current</c>、
    /// 不會刪除既有金鑰組，也不會呼叫執行階段更新；部署者必須另外執行明確發布。
    /// </remarks>
    public class KeySetGenerationService : IKeySetGenerationService
    {
        private static readonly Regex SafeUnifiedNameRegex = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        private readonly IKeyGenerationService _keyGenerationService;
        private readonly ICryptoService _cryptoService;

        /// <summary>建立使用指定單鍵產生器與低階密碼服務的金鑰組服務。</summary>
        /// <param name="keyGenerationService">產生 RSA 與 AES 模型的離線服務。</param>
        /// <param name="cryptoService">以 RSA OAEP 包裝新 AES 材料的服務。</param>
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
        /// <param name="unifiedName">可選的金鑰組名稱；只接受英數字元、底線與連字號。</param>
        /// <returns>包含 <c>.der</c>、<c>.public.pem</c>、<c>.private.pem</c> 三個 update 路徑的結果。</returns>
        /// <exception cref="ArgumentException">名稱含有不允許字元。</exception>
        /// <exception cref="InvalidOperationException"><see cref="CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="IOException">任何目標檔案已存在，或暫存／正式檔案無法寫入。</exception>
        /// <remarks>
        /// 呼叫會產生新的 RSA/AES 材料，先寫入唯一暫存檔，再依公開金鑰、私密金鑰、AES
        /// 順序移入正式名稱；AES 檔案最後出現是為了讓執行階段將完整三檔視為可發現的金鑰組。
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

            // 新金鑰組保留部署工具使用的既有檔名；舊版內容解析維持隔離在
            // KeyManager 內。
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

                // 最後發布 AES 檔案：KeyManager 以它作為發現標記，
                // 絕對不能觀察到不完整的三檔組合。
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
