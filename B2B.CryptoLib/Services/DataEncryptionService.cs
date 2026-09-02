using System;
using System.Security.Cryptography;
using System.Text;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace B2B.CryptoLib.Services
{
    /// <summary>
    /// 以指定金鑰組加解密文字，並以 <c>Base64.unifiedName</c> 格式封裝結果。
    /// </summary>
    /// <remarks>
    /// 新寫入資料使用 GCM v2 封裝：ASCII 魔術值 <c>B2BCGCM</c>、版本 2、
    /// 12 位元組隨機 nonce（隨機數）、密文 + 16 位元組標籤，並以統一名稱的 UTF-8
    /// 位元組作為 AAD。相同明文每次會因 nonce 隨機而產生不同密文；因此輸出不是
    /// 資料庫的確定性查找鍵。沒有 GCM 標記的既有載荷仍走
    /// AES-CBC/PKCS#7 相容解密分支。
    /// </remarks>
    public class DataEncryptionService : IDataEncryptionService
    {
        // 外層值仍維持 "Base64(payload).unifiedName"。此標記只存在於
        // Base64 載荷內，讓舊呼叫端持續使用相同契約。
        private static readonly byte[] GcmMagic = Encoding.ASCII.GetBytes("B2BCGCM");
        private const byte GcmPayloadVersion = 2;
        private const int GcmNonceLength = 12;
        private const int GcmTagLengthBits = 128;
        private const int GcmTagLengthBytes = GcmTagLengthBits / 8;

        private readonly ICryptoService _cryptoService;
        private readonly KeyManagerService _keyManagerService;

        /// <summary>
        /// 建立使用指定低階密碼服務與金鑰管理器的資料加密服務。
        /// </summary>
        /// <param name="cryptoService">執行舊版 AES-CBC 與 RSA 包裝的服務。</param>
        /// <param name="keyManagerService">解析統一名稱、載入金鑰組並管理快取的服務。</param>
        /// <exception cref="ArgumentNullException">任一相依服務為 <see langword="null"/>。</exception>
        public DataEncryptionService(ICryptoService cryptoService, KeyManagerService keyManagerService)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _keyManagerService = keyManagerService ?? throw new ArgumentNullException(nameof(keyManagerService));
        }

        /// <summary>
        /// 使用指定統一名稱的 AES 金鑰加密文字。
        /// </summary>
        /// <param name="plainText">要以 UTF-8 編碼的明文；<see langword="null"/> 或空字串回傳 <see langword="null"/>。</param>
        /// <param name="unifiedName">要使用的金鑰組名稱；不可為空且不可包含句點。</param>
        /// <returns>格式為 <c>Base64(payload).unifiedName</c> 的 GCM v2 密文；空輸入為 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException"><paramref name="unifiedName"/> 為空或含句點。</exception>
        /// <exception cref="InvalidOperationException">找不到名稱對應的完整 current/history 金鑰組。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">AES 金鑰長度不支援。</exception>
        /// <remarks>
        /// 每次呼叫都產生新的 12 位元組 nonce（隨機數），且將統一名稱綁定為 GCM AAD。
        /// 這讓尾綴名稱被竄改時訊息驗證失敗，也意味著不能以密文文字做等值查詢。
        /// </remarks>
        public string? Encrypt(string? plainText, string? unifiedName)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            if (string.IsNullOrEmpty(unifiedName) || unifiedName.Contains("."))
                throw new ArgumentException("unifiedName 必須提供且不可包含點號。", nameof(unifiedName));

            var key = _keyManagerService.GetAesKey(unifiedName);
            var encrypted = EncryptGcm(Encoding.UTF8.GetBytes(plainText), key, unifiedName);

            return Convert.ToBase64String(encrypted) + "." + unifiedName;
        }

        /// <summary>
        /// 解密 <c>Base64.unifiedName</c> 格式的資料。
        /// </summary>
        /// <param name="encryptedDataWithUnifiedName">包含 Base64 載荷與統一名稱尾綴的密文。</param>
        /// <returns>以 UTF-8 解碼的明文；<see langword="null"/> 或空輸入回傳 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException">缺少有效統一名稱尾綴，或名稱無法通過金鑰組安全規則。</exception>
        /// <exception cref="FormatException">載荷部分不是有效 Base64。</exception>
        /// <exception cref="InvalidOperationException">找不到尾綴名稱的完整金鑰組。</exception>
        /// <exception cref="CryptographicException">GCM 版本、nonce（隨機數）、標籤、AAD、RSA 包裝或舊版 CBC 解密失敗。</exception>
        /// <remarks>
        /// 有 <c>B2BCGCM</c> 標記的載荷必須符合 v2 封裝；沒有標記
        /// 才會交給舊版 AES-CBC/PKCS#7 讀取器。此相容分支不能移除，否則歷史資料無法讀取。
        /// </remarks>
        public string? Decrypt(string? encryptedDataWithUnifiedName)
        {
            if (string.IsNullOrEmpty(encryptedDataWithUnifiedName))
                return null;

            var unifiedName = GetUnifiedNameFromEncryptedData(encryptedDataWithUnifiedName);

            if (unifiedName is null)
                return null;
            var separator = encryptedDataWithUnifiedName.LastIndexOf('.');
            var encrypted = Convert.FromBase64String(encryptedDataWithUnifiedName.Substring(0, separator));
            var key = _keyManagerService.GetAesKey(unifiedName);
            var plain = HasGcmMagic(encrypted)
                ? DecryptGcm(encrypted, key, unifiedName)
                : _cryptoService.Decrypt(encrypted, CryptoAlgorithmType.AES, key);

            return Encoding.UTF8.GetString(plain);
        }

        /// <summary>
        /// 從加密字串的尾綴擷取統一金鑰名稱。
        /// </summary>
        /// <param name="encryptedDataWithUnifiedName">預期含有句點尾綴的加密字串。</param>
        /// <returns>最後一個句點後的統一名稱；空輸入為 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException">非空輸入沒有載荷，或句點後沒有名稱。</exception>
        /// <remarks>方法只解析外層分隔符，不驗證 Base64、訊息驗證或金鑰組是否存在。</remarks>
        public string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName)
        {
            if (string.IsNullOrEmpty(encryptedDataWithUnifiedName))
                return null;

            var index = encryptedDataWithUnifiedName.LastIndexOf('.');

            if (index <= 0 || index == encryptedDataWithUnifiedName.Length - 1)
                throw new ArgumentException("加密字串格式不正確，缺少有效的 unifiedName 尾綴。", nameof(encryptedDataWithUnifiedName));

            return encryptedDataWithUnifiedName.Substring(index + 1);
        }

        /// <summary>
        /// 判斷字串是否符合可解碼的加密資料格式。
        /// </summary>
        /// <param name="data">候選的 <c>Base64(payload).unifiedName</c> 字串。</param>
        /// <returns>載荷可被 Base64 解碼且有非空尾綴時為 <see langword="true"/>。</returns>
        /// <remarks>
        /// 這是語法檢查，不是訊息驗證、授權、金鑰存在性或可解密性檢查；
        /// 呼叫端不能用它判定資料可信或允許資料庫操作。
        /// </remarks>
        public bool IsValidEncryptedFormat(string? data)
        {
            if (string.IsNullOrEmpty(data))
                return false;

            try
            {
                var index = data.LastIndexOf('.');

                if (index <= 0 || index == data.Length - 1)
                    return false;

                Convert.FromBase64String(data.Substring(0, index));

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] EncryptGcm(byte[] plain, SymmetricKeyModel key, string unifiedName)
        {
            ValidateGcmKey(key);

            var nonce = new byte[GcmNonceLength];

            RandomNumberGenerator.Fill(nonce);

            // 保留既有 v2 封裝欄位，並將外部名稱綁定為 AAD；任一項變更
            // 都會使既有密文無法驗證。
            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(key.Key), GcmTagLengthBits, nonce, Encoding.UTF8.GetBytes(unifiedName)));

            var cipherTextAndTag = new byte[cipher.GetOutputSize(plain.Length)];
            var length = cipher.ProcessBytes(plain, 0, plain.Length, cipherTextAndTag, 0);
            length += cipher.DoFinal(cipherTextAndTag, length);

            var payload = new byte[GcmMagic.Length + 1 + nonce.Length + length];
            Buffer.BlockCopy(GcmMagic, 0, payload, 0, GcmMagic.Length);
            payload[GcmMagic.Length] = GcmPayloadVersion;
            Buffer.BlockCopy(nonce, 0, payload, GcmMagic.Length + 1, nonce.Length);
            Buffer.BlockCopy(cipherTextAndTag, 0, payload, GcmMagic.Length + 1 + nonce.Length, length);

            return payload;
        }

        private static byte[] DecryptGcm(byte[] payload, SymmetricKeyModel key, string unifiedName)
        {
            ValidateGcmKey(key);

            var headerLength = GcmMagic.Length + 1;

            if (payload[GcmMagic.Length] != GcmPayloadVersion)
                throw new CryptographicException("不支援的加密資料版本。");

            if (payload.Length < headerLength + GcmNonceLength + GcmTagLengthBytes)
                throw new CryptographicException("加密資料格式不正確。");

            var nonce = new byte[GcmNonceLength];
            Buffer.BlockCopy(payload, headerLength, nonce, 0, nonce.Length);

            var cipherTextOffset = headerLength + nonce.Length;
            var cipherTextLength = payload.Length - cipherTextOffset;
            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(key.Key), GcmTagLengthBits, nonce, Encoding.UTF8.GetBytes(unifiedName)));

            var plain = new byte[cipher.GetOutputSize(cipherTextLength)];

            try
            {
                var length = cipher.ProcessBytes(payload, cipherTextOffset, cipherTextLength, plain, 0);
                length += cipher.DoFinal(plain, length);

                if (length == plain.Length)
                    return plain;

                var result = new byte[length];
                Buffer.BlockCopy(plain, 0, result, 0, length);
                return result;
            }
            catch (InvalidCipherTextException ex)
            {
                throw new CryptographicException("加密資料驗證失敗。", ex);
            }
        }

        private static bool HasGcmMagic(byte[] payload)
        {
            if (payload == null || payload.Length < GcmMagic.Length + 1)
                return false;

            for (var i = 0; i < GcmMagic.Length; i++)
                if (payload[i] != GcmMagic[i])
                    return false;

            return true;
        }

        private static void ValidateGcmKey(SymmetricKeyModel key)
        {
            if (key == null || key.Key == null || (key.Key.Length != 16 && key.Key.Length != 24 && key.Key.Length != 32))
                throw new CryptographicException("AES 金鑰長度必須為 128、192 或 256 位元。");
        }
    }
}
