using System;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Services;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// CryptoSuite 對外領域入口，使用目前啟用的金鑰組處理文字資料。
    /// </summary>
    /// <remarks>
    /// 這個 legacy domain wrapper 會從 <see cref="KeyManagerService.GetLatestActiveUnifiedName"/>
    /// 取得 Current 中排序後的最新名稱；需要明確且可隔離的 context 時，請優先使用
    /// <see cref="CryptoClient"/>。建構函式不會替呼叫端啟動金鑰更新。
    /// </remarks>
    public class CryptoSuiteDomain
    {
        private readonly KeyManagerService _keyManagerService;
        private readonly IDataEncryptionService _dataEncryptionService;

        /// <summary>
        /// 建立使用指定金鑰管理器與資料加密服務的 domain wrapper。
        /// </summary>
        /// <param name="keyManagerService">提供 active key-set 查找的服務。</param>
        /// <param name="dataEncryptionService">實際執行文字封裝加解密的服務。</param>
        /// <exception cref="ArgumentNullException">任一相依服務為 <see langword="null"/>。</exception>
        public CryptoSuiteDomain(KeyManagerService keyManagerService, IDataEncryptionService dataEncryptionService)
        {
            _keyManagerService = keyManagerService ?? throw new ArgumentNullException(nameof(keyManagerService));
            _dataEncryptionService = dataEncryptionService ?? throw new ArgumentNullException(nameof(dataEncryptionService));
        }

        /// <summary>
        /// 以 Current 中的最新 active unified name 加密文字。
        /// </summary>
        /// <param name="plainText">要加密的 UTF-8 文字；空值或空字串的結果由底層服務定義為 <see langword="null"/>。</param>
        /// <returns>封裝成 <c>Base64(payload).unifiedName</c> 的密文，或空輸入的 <see langword="null"/>。</returns>
        /// <exception cref="InvalidOperationException">Current 沒有完整可用的金鑰組。</exception>
        public string? EncryptByCryptoSuite(string? plainText) => _dataEncryptionService.Encrypt(plainText, _keyManagerService.GetLatestActiveUnifiedName());

        /// <summary>
        /// 解密以 unified name 尾綴封裝的文字。
        /// </summary>
        /// <param name="encryptedText">預期為 <c>Base64(payload).unifiedName</c> 的密文。</param>
        /// <returns>解密後的 UTF-8 文字，或空輸入的 <see langword="null"/>。</returns>
        /// <exception cref="ArgumentException">外層格式無效。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">authentication 或解密失敗。</exception>
        public string? DecryptByCryptoSuite(string? encryptedText) => _dataEncryptionService.Decrypt(encryptedText);
    }
}
