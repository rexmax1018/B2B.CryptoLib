using System;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Services;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// CryptoSuite 對外領域入口，使用目前啟用的金鑰組處理文字資料。
    /// </summary>
    public class CryptoSuiteDomain
    {
        private readonly KeyManagerService _keyManagerService; private readonly IDataEncryptionService _dataEncryptionService;

        public CryptoSuiteDomain(KeyManagerService keyManagerService, IDataEncryptionService dataEncryptionService)
        {
            _keyManagerService = keyManagerService ?? throw new ArgumentNullException(nameof(keyManagerService));
            _dataEncryptionService = dataEncryptionService ?? throw new ArgumentNullException(nameof(dataEncryptionService));
        }

        public string EncryptByCryptoSuite(string plainText) => _dataEncryptionService.Encrypt(plainText, _keyManagerService.GetLatestActiveUnifiedName());

        public string DecryptByCryptoSuite(string encryptedText) => _dataEncryptionService.Decrypt(encryptedText);
    }
}
