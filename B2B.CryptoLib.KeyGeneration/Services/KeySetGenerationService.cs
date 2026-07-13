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
    public class KeySetGenerationService : IKeySetGenerationService
    {
        private static readonly Regex SafeUnifiedNameRegex = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        private readonly IKeyGenerationService _keyGenerationService;
        private readonly ICryptoService _cryptoService;

        public KeySetGenerationService(IKeyGenerationService keyGenerationService, ICryptoService cryptoService)
        {
            _keyGenerationService = keyGenerationService ?? throw new ArgumentNullException(nameof(keyGenerationService));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        }

        /// <summary>
        /// 產生一組使用舊版檔名、但採新版 OAEP 與 Key:IV 內容的檔案到 KeyDirectory/update。
        /// 未指定名稱時，使用 GenerateKeyFileName 產生的八碼字串作為共同前綴。
        /// </summary>
        public KeySetGenerationResult GenerateAndSave(string unifiedName = null)
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

            // 新產生的金鑰組維持目前規則；舊版格式僅由 KeyManager 的讀取相容分支處理。
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

                // The AES file is published last because KeyManager uses it to discover key sets.
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

        private static string NormalizeUnifiedName(string unifiedName)
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
