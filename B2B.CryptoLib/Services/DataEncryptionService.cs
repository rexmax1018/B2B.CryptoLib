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

        public DataEncryptionService(ICryptoService cryptoService, KeyManagerService keyManagerService)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _keyManagerService = keyManagerService ?? throw new ArgumentNullException(nameof(keyManagerService));
        }

        /// <summary>
        /// 使用指定統一名稱的 AES 金鑰加密文字。
        /// </summary>
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
