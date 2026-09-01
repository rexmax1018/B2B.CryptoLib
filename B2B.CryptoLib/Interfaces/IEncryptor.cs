namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 通用加解密介面。
    /// </summary>
    /// <remarks>
    /// 介面只定義位元組進出，不定義金鑰儲存、文字編碼或封裝格式。呼叫端
    /// 必須依實作所宣告的演算法處理資料長度、填充與例外；不要把不同演算法
    /// 產生的密文混用。
    /// </remarks>
    public interface IEncryptor
    {
        /// <summary>加密一段位元組。</summary>
        /// <param name="data">要加密的資料。</param>
        /// <returns>加密後的位元組。</returns>
        /// <exception cref="System.ArgumentNullException">輸入資料為 null。</exception>
        byte[] Encrypt(byte[] data);

        /// <summary>解密一段位元組。</summary>
        /// <param name="encryptedData">要解密的資料。</param>
        /// <returns>解密後的位元組。</returns>
        /// <exception cref="System.ArgumentNullException">輸入資料為 null。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">密文無法通過演算法的解密或完整性檢查。</exception>
        byte[] Decrypt(byte[] encryptedData);
    }
}
