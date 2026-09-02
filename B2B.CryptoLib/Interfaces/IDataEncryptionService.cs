namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 以統一金鑰名稱封裝資料加解密的服務介面。
    /// </summary>
    /// <remarks>
    /// 高階格式固定為 <c>Base64(payload).unifiedName</c>。新的載荷使用
    /// GCM v2；沒有 <c>B2BCGCM</c> 標記的輸入仍走舊版 AES-CBC/PKCS#7
    /// 讀取分支。此介面不提供資料庫確定性查找語意。
    /// </remarks>
    public interface IDataEncryptionService
    {
        /// <summary>使用指定統一名稱產生 GCM v2 封裝密文。</summary>
        /// <param name="plainText">要加密的文字；null 或空字串回傳 null。</param>
        /// <param name="unifiedName">金鑰組名稱；不可為空且不可包含句點。</param>
        /// <returns>格式為 <c>Base64(payload).unifiedName</c> 的密文，或空輸入的 null。</returns>
        /// <exception cref="System.ArgumentException">名稱無效或缺少完整金鑰組。</exception>
        /// <exception cref="System.InvalidOperationException">指定金鑰組不存在。</exception>
        string? Encrypt(string? plainText, string? unifiedName);

        /// <summary>解密 GCM v2 或相容舊版 AES-CBC 封裝密文。</summary>
        /// <param name="encryptedDataWithUnifiedName">格式為 <c>Base64(payload).unifiedName</c> 的密文。</param>
        /// <returns>UTF-8 明文；null 或空輸入回傳 null。</returns>
        /// <exception cref="System.ArgumentException">外層或統一名稱格式無效。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">GCM 訊息驗證、RSA 包裝或舊版 CBC 解密失敗。</exception>
        /// <exception cref="System.InvalidOperationException">找不到尾綴名稱的完整金鑰組。</exception>
        string? Decrypt(string? encryptedDataWithUnifiedName);

        /// <summary>從密文外層尾綴讀取統一名稱，不執行解密。</summary>
        /// <param name="encryptedDataWithUnifiedName">候選加密字串。</param>
        /// <returns>尾綴名稱；null 或空輸入回傳 null。</returns>
        /// <exception cref="System.ArgumentException">缺少載荷或非空統一名稱尾綴。</exception>
        string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName);

        /// <summary>只檢查外層 Base64 與尾綴形狀。</summary>
        /// <param name="data">候選加密字串。</param>
        /// <returns>外層可解析時為 true；不代表訊息驗證、授權或可解密性。</returns>
        bool IsValidEncryptedFormat(string? data);
    }
}
