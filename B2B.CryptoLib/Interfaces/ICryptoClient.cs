using System.Threading.Tasks;

namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 不需要 DI 的高階文字加解密用戶端。
    /// </summary>
    /// <remarks>
    /// 每個執行個體都有自己的金鑰根目錄、啟用的統一名稱與快取；它不會
    /// 使用 <see cref="B2B.CryptoLib.Config.CryptoConfig"/> 的程序層級設定。建構完成後不會
    /// 自動處理 <c>update</c> 目錄；請顯式呼叫 <see cref="UpdateKeySetsAsync"/>。
    /// <para>
    /// 相同執行個體的查找與更新由內部鎖序列化，讓更新完成前不會把半組檔案交給
    /// 同一個用戶端；但不同執行個體或不同程序沒有跨執行脈絡協調鎖。
    /// </para>
    /// </remarks>
    public interface ICryptoClient
    {
        /// <summary>使用建立用戶端時設定的啟用統一名稱加密文字。</summary>
        /// <param name="plainText">要加密的文字；null 或空字串回傳 null。</param>
        /// <returns>格式為 <c>Base64(payload).unifiedName</c> 的密文，或空輸入的 null。</returns>
        /// <exception cref="System.InvalidOperationException">未設定啟用名稱，或找不到對應的完整金鑰組。</exception>
        string? Encrypt(string? plainText);

        /// <summary>使用明確統一名稱加密文字。</summary>
        /// <param name="plainText">要加密的文字；null 或空字串回傳 null。</param>
        /// <param name="unifiedName">要使用的金鑰組名稱；不可為空且不可包含句點。</param>
        /// <returns>格式為 <c>Base64(payload).unifiedName</c> 的密文，或空輸入的 null。</returns>
        /// <exception cref="System.ArgumentException">名稱無效，或無法找到完整金鑰組。</exception>
        string? Encrypt(string? plainText, string? unifiedName);

        /// <summary>解密包含統一名稱尾綴的文字密文。</summary>
        /// <param name="encryptedDataWithUnifiedName">格式為 <c>Base64(payload).unifiedName</c> 的密文。</param>
        /// <returns>解密後的 UTF-8 文字；null 或空字串回傳 null。</returns>
        /// <exception cref="System.ArgumentException">外層格式或統一名稱尾綴無效。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">訊息驗證或解密失敗。</exception>
        string? Decrypt(string? encryptedDataWithUnifiedName);

        /// <summary>
        /// 僅驗證外層格式是否可解析；不代表資料已通過訊息驗證或可使用目前金鑰解密。
        /// </summary>
        /// <param name="data">候選的 <c>Base64(payload).unifiedName</c> 字串。</param>
        /// <returns>外層格式可解析時為 true；不保證標籤、AAD、金鑰或授權。</returns>
        bool IsValidEncryptedFormat(string? data);

        /// <summary>從密文尾綴擷取統一名稱。</summary>
        /// <param name="encryptedDataWithUnifiedName">包含載荷與名稱尾綴的密文。</param>
        /// <returns>尾綴中的名稱；null 或空輸入回傳 null。</returns>
        /// <exception cref="System.ArgumentException">缺少非空載荷或名稱尾綴。</exception>
        string? GetUnifiedName(string? encryptedDataWithUnifiedName);

        /// <summary>取得統一名稱的相容命名版本。</summary>
        /// <param name="encryptedDataWithUnifiedName">包含載荷與名稱尾綴的密文。</param>
        /// <returns>與 <see cref="GetUnifiedName(string?)"/> 相同的名稱或 null。</returns>
        /// <exception cref="System.ArgumentException">密文尾綴格式無效。</exception>
        string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName);

        /// <summary>明確執行一次 update 金鑰組掃描、發布與快取失效。</summary>
        /// <returns>更新完成後完成的工作。</returns>
        /// <remarks>這是唯一由高階用戶端觸發檔案發布的生命週期操作；建立用戶端不會自動執行。</remarks>
        Task UpdateKeySetsAsync();
    }
}
