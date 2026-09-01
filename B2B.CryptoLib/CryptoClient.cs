using System;
using System.Threading.Tasks;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;
using B2B.CryptoLib.Services;

namespace B2B.CryptoLib
{
    /// <summary>
    /// 封裝一組彼此隔離的執行階段密碼執行脈絡。
    /// </summary>
    /// <remarks>
    /// <see cref="CryptoClient"/> 不依賴 Autofac，也不讀取程序層級的
    /// <see cref="Config.CryptoConfig"/>。每個執行個體都擁有自己的
    /// <see cref="KeyManagerService"/>、金鑰快取、目錄根與啟用的統一名稱，
    /// 適合多租戶或同一程序內需要多組金鑰的宿主程式。
    /// <para>
    /// 建構用戶端只建立目錄並保留執行階段執行脈絡，不會掃描、發布或消費
    /// <c>update</c> 目錄；請以 <see cref="UpdateKeySetsAsync"/> 明確執行發布。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var client = CryptoClient.Create(new CryptoOptions
    /// {
    ///     KeyManagerBasePath = @"C:\CryptoKeys\tenant-a",
    ///     ActiveUnifiedName = "tenant-a-current"
    /// });
    /// var encrypted = client.Encrypt("secret");
    /// var plainText = client.Decrypt(encrypted);
    /// </code>
    /// </example>
    public sealed class CryptoClient : ICryptoClient
    {
        private readonly IDataEncryptionService _dataEncryptionService;
        private readonly KeyManagerService? _keyManagerService;
        private readonly string? _activeUnifiedName;

        /// <summary>
        /// 以明確的執行階段選項建立不依賴 Autofac 的用戶端。
        /// </summary>
        /// <param name="options">包含金鑰管理根目錄及可選啟用統一名稱的設定。</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> 為 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException">設定中的路徑無效，或啟用的統一名稱不符合安全名稱規則。</exception>
        /// <remarks>
        /// 此建構函式建立執行個體專屬的執行脈絡。它不會呼叫
        /// <see cref="KeyManagerService.StartAsync"/>，所以待發布檔案必須由呼叫端
        /// 在適當時機明確處理。
        /// </remarks>
        public CryptoClient(CryptoOptions options)
        {
            var normalized = (options ?? throw new ArgumentNullException(nameof(options))).Normalize();
            var cryptoService = new CryptoService();
            // 建構金鑰管理器會建立三個目錄根；update 消費維持明確呼叫，
            // 讓建構行為具有可預期的生命週期副作用。
            var keyManagerService = new KeyManagerService(normalized.KeyManagerBasePath, cryptoService);

            _dataEncryptionService = new DataEncryptionService(cryptoService, keyManagerService);
            _keyManagerService = keyManagerService;
            _activeUnifiedName = normalized.ActiveUnifiedName;
        }

        /// <summary>
        /// 建立一組新的、彼此隔離的執行階段密碼執行脈絡。
        /// </summary>
        /// <param name="options">要套用到新用戶端的執行階段設定。</param>
        /// <returns>新的、與其他 <see cref="CryptoClient"/> 執行個體隔離的用戶端。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> 為 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException">設定中的路徑無效，或啟用的統一名稱不符合安全名稱規則。</exception>
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
        /// 明確執行一次 Update 金鑰組發布；建構用戶端時不會自動掃描或消費 Update 檔案。
        /// </summary>
        /// <returns>在同步完成一次更新掃描與快取失效後完成的工作。</returns>
        /// <exception cref="InvalidOperationException">此執行個體沒有可供更新的 <see cref="KeyManagerService"/>（僅限內部測試建構路徑）。</exception>
        /// <remarks>
        /// 完整金鑰組會依公開金鑰、私鑰、AES 材料的順序發布，
        /// 並在成功後清除該執行個體的快取。此方法不提供跨程序或跨
        /// <see cref="CryptoClient"/> 執行個體的協調鎖。
        /// </remarks>
        public Task UpdateKeySetsAsync()
        {
            if (_keyManagerService is null)
                throw new InvalidOperationException("This CryptoClient is not associated with a KeyManagerService.");

            return _keyManagerService.StartAsync();
        }

        /// <inheritdoc />
        /// <param name="plainText">要加密的文字；<see langword="null"/> 或空字串會回傳 <see langword="null"/>。</param>
        /// <returns>以啟用的統一名稱封裝的 <c>Base64(payload).unifiedName</c>；空輸入為 <see langword="null"/>。</returns>
        /// <exception cref="InvalidOperationException">未設定啟用的統一名稱，或對應金鑰組不存在。</exception>
        /// <remarks>此多載不會猜測或排序 Current 中的金鑰；啟用名稱必須由選項明確指定。</remarks>
        public string? Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            if (_activeUnifiedName is null)
                throw new InvalidOperationException("No ActiveUnifiedName was configured. Provide a unifiedName to Encrypt or configure CryptoOptions.ActiveUnifiedName.");

            return _dataEncryptionService.Encrypt(plainText, _activeUnifiedName);
        }

        /// <inheritdoc />
        /// <param name="plainText">要加密的文字；<see langword="null"/> 或空字串會回傳 <see langword="null"/>。</param>
        /// <param name="unifiedName">要使用的金鑰組名稱；不可為空且不可包含句點。</param>
        /// <returns>以指定名稱封裝的 <c>Base64(payload).unifiedName</c>；空輸入為 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException"><paramref name="unifiedName"/> 無效，或無法在金鑰管理目錄找到完整金鑰組。</exception>
        /// <remarks>指定名稱會成為 GCM 的 UTF-8 AAD 與輸出尾綴；它不會被啟用名稱取代。</remarks>
        public string? Encrypt(string? plainText, string? unifiedName) => _dataEncryptionService.Encrypt(plainText, unifiedName);

        /// <inheritdoc />
        /// <param name="encryptedDataWithUnifiedName">以 <c>Base64(payload).unifiedName</c> 表示的密文。</param>
        /// <returns>解密後的 UTF-8 文字；<see langword="null"/> 或空輸入回傳 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException">外層格式無效或統一名稱尾綴缺失。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">GCM 訊息驗證、RSA 解密或舊版 CBC 解密失敗。</exception>
        /// <exception cref="InvalidOperationException">指定名稱沒有可用的金鑰組。</exception>
        public string? Decrypt(string? encryptedDataWithUnifiedName) => _dataEncryptionService.Decrypt(encryptedDataWithUnifiedName);

        /// <summary>
        /// 僅驗證外層格式，不執行訊息驗證或解密。
        /// </summary>
        /// <param name="data">要檢查的候選加密字串。</param>
        /// <returns>外層 Base64 與統一名稱形狀可解析時為 <see langword="true"/>；不保證完整性、授權或可解密性。</returns>
        /// <remarks>不要以此結果作為訊息驗證、授權或資料庫查詢成功的判定。</remarks>
        public bool IsValidEncryptedFormat(string? data) => _dataEncryptionService.IsValidEncryptedFormat(data);

        /// <inheritdoc />
        /// <param name="encryptedDataWithUnifiedName">包含統一名稱尾綴的加密字串。</param>
        /// <returns>尾綴中的統一名稱；空輸入為 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException">字串缺少非空的載荷或統一名稱尾綴。</exception>
        public string? GetUnifiedName(string? encryptedDataWithUnifiedName) => _dataEncryptionService.GetUnifiedNameFromEncryptedData(encryptedDataWithUnifiedName);

        /// <inheritdoc />
        /// <param name="encryptedDataWithUnifiedName">包含統一名稱尾綴的加密字串。</param>
        /// <returns>與 <see cref="GetUnifiedName(string?)"/> 相同的結果。</returns>
        /// <exception cref="ArgumentException">字串缺少非空的載荷或統一名稱尾綴。</exception>
        /// <remarks>保留此名稱是為了相容既有呼叫端；新程式可使用較短的 <see cref="GetUnifiedName(string?)"/>。</remarks>
        public string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName) => GetUnifiedName(encryptedDataWithUnifiedName);
    }
}
