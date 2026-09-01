using System;
using System.Threading.Tasks;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;
using B2B.CryptoLib.Services;

namespace B2B.CryptoLib
{
    /// <summary>
    /// 封裝一組彼此隔離的 runtime crypto context。
    /// </summary>
    public sealed class CryptoClient : ICryptoClient
    {
        private readonly IDataEncryptionService _dataEncryptionService;
        private readonly KeyManagerService? _keyManagerService;
        private readonly string? _activeUnifiedName;

        /// <summary>
        /// 以明確 runtime options 建立不依賴 Autofac 的 client。
        /// </summary>
        public CryptoClient(CryptoOptions options)
        {
            var normalized = (options ?? throw new ArgumentNullException(nameof(options))).Normalize();
            var cryptoService = new CryptoService();
            var keyManagerService = new KeyManagerService(normalized.KeyManagerBasePath, cryptoService);

            _dataEncryptionService = new DataEncryptionService(cryptoService, keyManagerService);
            _keyManagerService = keyManagerService;
            _activeUnifiedName = normalized.ActiveUnifiedName;
        }

        /// <summary>
        /// 建立一組新的、彼此隔離的 runtime crypto context。
        /// </summary>
        public static CryptoClient Create(CryptoOptions options) => new CryptoClient(options);

        internal CryptoClient(IDataEncryptionService dataEncryptionService)
            : this(dataEncryptionService, null, null)
        {
        }

        internal CryptoClient(IDataEncryptionService dataEncryptionService, string? activeUnifiedName)
            : this(dataEncryptionService, null, activeUnifiedName)
        {
        }

        internal CryptoClient(IDataEncryptionService dataEncryptionService, KeyManagerService? keyManagerService, string? activeUnifiedName)
        {
            _dataEncryptionService = dataEncryptionService ?? throw new ArgumentNullException(nameof(dataEncryptionService));
            _keyManagerService = keyManagerService;
            _activeUnifiedName = activeUnifiedName;
        }

        /// <summary>
        /// 明確執行一次 Update 金鑰組發布；建構 client 時不會自動掃描或消費 Update 檔案。
        /// </summary>
        public Task UpdateKeySetsAsync()
        {
            if (_keyManagerService is null)
                throw new InvalidOperationException("This CryptoClient is not associated with a KeyManagerService.");

            return _keyManagerService.StartAsync();
        }

        /// <inheritdoc />
        public string? Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            if (_activeUnifiedName is null)
                throw new InvalidOperationException("No ActiveUnifiedName was configured. Provide a unifiedName to Encrypt or configure CryptoOptions.ActiveUnifiedName.");

            return _dataEncryptionService.Encrypt(plainText, _activeUnifiedName);
        }

        /// <inheritdoc />
        public string? Encrypt(string? plainText, string? unifiedName) => _dataEncryptionService.Encrypt(plainText, unifiedName);

        /// <inheritdoc />
        public string? Decrypt(string? encryptedDataWithUnifiedName) => _dataEncryptionService.Decrypt(encryptedDataWithUnifiedName);

        /// <summary>
        /// 僅驗證外層格式，不執行 authentication 或解密。
        /// </summary>
        public bool IsValidEncryptedFormat(string? data) => _dataEncryptionService.IsValidEncryptedFormat(data);

        /// <inheritdoc />
        public string? GetUnifiedName(string? encryptedDataWithUnifiedName) => _dataEncryptionService.GetUnifiedNameFromEncryptedData(encryptedDataWithUnifiedName);

        /// <inheritdoc />
        public string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName) => GetUnifiedName(encryptedDataWithUnifiedName);
    }
}
