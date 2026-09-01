using System.Threading.Tasks;

namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 不需要 DI 的高階文字加解密 client。
    /// </summary>
    public interface ICryptoClient
    {
        string? Encrypt(string? plainText);

        string? Encrypt(string? plainText, string? unifiedName);

        string? Decrypt(string? encryptedDataWithUnifiedName);

        /// <summary>
        /// 僅驗證外層格式是否可解析；不代表資料已通過 authentication 或可使用目前金鑰解密。
        /// </summary>
        bool IsValidEncryptedFormat(string? data);

        string? GetUnifiedName(string? encryptedDataWithUnifiedName);

        string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName);

        Task UpdateKeySetsAsync();
    }
}
