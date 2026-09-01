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
    /// 新寫入資料使用 GCM v2 envelope：ASCII magic <c>B2BCGCM</c>、version 2、
    /// 12-byte random nonce、ciphertext + 16-byte tag，並以 unified name 的 UTF-8
    /// bytes 作為 AAD。相同明文每次會因 nonce 隨機而產生不同密文；因此輸出不是
    /// deterministic database lookup key。沒有 GCM marker 的既有 payload 仍走
    /// AES-CBC/PKCS#7 相容解密分支。
    /// </remarks>
    public class DataEncryptionService : IDataEncryptionService
    {
        // The outer value remains "Base64(payload).unifiedName". This marker only
        // exists inside the Base64 payload so old callers keep the same contract.
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
        /// <param name="cryptoService">執行 legacy AES-CBC 與 RSA wrapping 的服務。</param>
        /// <param name="keyManagerService">解析 unified name、載入 key set 並管理 cache 的服務。</param>
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
        /// <param name="unifiedName">要使用的 key-set 名稱；不可為空且不可包含句點。</param>
        /// <returns>格式為 <c>Base64(payload).unifiedName</c> 的 GCM v2 密文；空輸入為 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException"><paramref name="unifiedName"/> 為空或含句點。</exception>
        /// <exception cref="InvalidOperationException">找不到名稱對應的完整 current/history key set。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">AES key 長度不支援。</exception>
        /// <remarks>
        /// 每次呼叫都產生新的 12-byte nonce，且將 unified name 綁定為 GCM AAD。
        /// 這讓尾綴名稱被竄改時 authentication 失敗，也意味著不能以密文文字做等值查詢。
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
        /// <param name="encryptedDataWithUnifiedName">包含 Base64 payload 與 unified name 尾綴的密文。</param>
        /// <returns>以 UTF-8 解碼的明文；<see langword="null"/> 或空輸入回傳 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException">缺少有效 unified name 尾綴，或名稱無法通過 key-set 安全規則。</exception>
        /// <exception cref="FormatException">payload 部分不是有效 Base64。</exception>
        /// <exception cref="InvalidOperationException">找不到尾綴名稱的完整 key set。</exception>
        /// <exception cref="CryptographicException">GCM version、nonce、tag、AAD、RSA wrapping 或 legacy CBC 解密失敗。</exception>
        /// <remarks>
        /// 有 <c>B2BCGCM</c> marker 的 payload 必須符合 v2 envelope；沒有 marker
        /// 才會交給 legacy AES-CBC/PKCS#7 reader。此相容分支不能移除，否則歷史資料無法讀取。
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
        /// <returns>最後一個句點後的 unified name；空輸入為 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException">非空輸入沒有 payload，或句點後沒有名稱。</exception>
        /// <remarks>方法只解析外層分隔符，不驗證 Base64、authentication 或 key set 是否存在。</remarks>
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
        /// <returns>payload 可被 Base64 解碼且有非空尾綴時為 <see langword="true"/>。</returns>
        /// <remarks>
        /// 這是 syntax check，不是 authentication、授權、key existence 或 decryptability check；
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

            // Keep the established v2 envelope fields and bind the external name
            // as AAD; changing either would make existing ciphertext unverifiable.
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
