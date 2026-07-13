using System;
using System.Text;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib.Services
{
    /// <summary>
    /// 以指定金鑰組加解密文字，並以 <c>Base64.unifiedName</c> 格式封裝結果。
    /// </summary>
    public class DataEncryptionService : IDataEncryptionService
    {
        private readonly ICryptoService _cryptoService; private readonly KeyManagerService _keyManagerService;

        public DataEncryptionService(ICryptoService cryptoService, KeyManagerService keyManagerService)
        {
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
            _keyManagerService = keyManagerService ?? throw new ArgumentNullException(nameof(keyManagerService));
        }

        /// <summary>
        /// 使用指定統一名稱的 AES 金鑰加密文字。
        /// </summary>
        public string Encrypt(string plainText, string unifiedName)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            if (string.IsNullOrEmpty(unifiedName) || unifiedName.Contains("."))
                throw new ArgumentException("unifiedName 必須提供且不可包含點號。", nameof(unifiedName));

            var key = _keyManagerService.GetAesKey(unifiedName);
            var encrypted = _cryptoService.Encrypt(Encoding.UTF8.GetBytes(plainText), CryptoAlgorithmType.AES, key);

            return Convert.ToBase64String(encrypted) + "." + unifiedName;
        }

        /// <summary>
        /// 解密 <c>Base64.unifiedName</c> 格式的資料。
        /// </summary>
        public string Decrypt(string encryptedDataWithUnifiedName)
        {
            if (string.IsNullOrEmpty(encryptedDataWithUnifiedName))
                return null;

            var unifiedName = GetUnifiedNameFromEncryptedData(encryptedDataWithUnifiedName);
            var separator = encryptedDataWithUnifiedName.LastIndexOf('.');
            var encrypted = Convert.FromBase64String(encryptedDataWithUnifiedName.Substring(0, separator));
            var key = _keyManagerService.GetAesKey(unifiedName);

            return Encoding.UTF8.GetString(_cryptoService.Decrypt(encrypted, CryptoAlgorithmType.AES, key));
        }

        /// <summary>
        /// 從加密字串的尾綴擷取統一金鑰名稱。
        /// </summary>
        public string GetUnifiedNameFromEncryptedData(string encryptedDataWithUnifiedName)
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
        public bool IsValidEncryptedFormat(string data)
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
    }
}
