using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;

namespace B2B.CryptoLib.Extensions
{
    /// <summary>
    /// 為 <see cref="ICryptoService"/> 提供 Encrypt、Decrypt、Sign 與 Verify 語法糖。
    /// </summary>
    /// <remarks>
    /// 擴充方法不建立 service、不保存 key，也不改變底層的演算法與 key-model 配對規則。
    /// 它們只是把 service 放在最後一個參數，讓既有 byte-array pipeline 更易讀。
    /// </remarks>
    public static class CryptoExtensions
    {
        /// <summary>以指定服務及 key model 加密 bytes。</summary>
        /// <typeparam name="T">與 <paramref name="alg"/> 相容的 key model 類型。</typeparam>
        /// <param name="data">要加密的資料；不可為 <see langword="null"/>。</param>
        /// <param name="alg">要使用的 <see cref="CryptoAlgorithmType"/>。</param>
        /// <param name="keyModel">演算法所需的 key model。</param>
        /// <param name="service">實際執行加密的 service。</param>
        /// <returns>加密後的 bytes。</returns>
        /// <exception cref="System.ArgumentNullException">資料為 null，或 service/key model 不可用。</exception>
        /// <exception cref="System.NotSupportedException">演算法與 model 不相容。</exception>
        public static byte[] EncryptWith<T>(this byte[] data, CryptoAlgorithmType alg, T keyModel, ICryptoService service) where T : class => service.Encrypt(data, alg, keyModel);

        /// <summary>以指定服務及 key model 解密 bytes。</summary>
        /// <typeparam name="T">與 <paramref name="alg"/> 相容的 key model 類型。</typeparam>
        /// <param name="encrypted">要解密的資料；不可為 <see langword="null"/>。</param>
        /// <param name="alg">要使用的 <see cref="CryptoAlgorithmType"/>。</param>
        /// <param name="keyModel">演算法所需的 key model。</param>
        /// <param name="service">實際執行解密的 service。</param>
        /// <returns>解密後的 bytes。</returns>
        /// <exception cref="System.ArgumentNullException">資料為 null，或 service/key model 不可用。</exception>
        /// <exception cref="System.NotSupportedException">演算法與 model 不相容。</exception>
        public static byte[] DecryptWith<T>(this byte[] encrypted, CryptoAlgorithmType alg, T keyModel, ICryptoService service) where T : class => service.Decrypt(encrypted, alg, keyModel);

        /// <summary>以指定私鑰對 bytes 產生簽章。</summary>
        /// <typeparam name="T">與 RSA 或 ECC 簽章路徑相容的 key model 類型。</typeparam>
        /// <param name="data">要簽章的資料。</param>
        /// <param name="alg">簽章演算法類型。</param>
        /// <param name="privateKey">含私鑰 PEM 的 model。</param>
        /// <param name="service">實際執行簽章的 service。</param>
        /// <returns>產生的數位簽章 bytes。</returns>
        /// <exception cref="System.ArgumentNullException">必要輸入為 null。</exception>
        /// <exception cref="System.NotSupportedException">model 或演算法不支援簽章。</exception>
        public static byte[] SignWith<T>(this byte[] data, CryptoAlgorithmType alg, T privateKey, ICryptoService service) where T : class => service.Sign(data, alg, privateKey);

        /// <summary>以指定公鑰驗證 bytes 的數位簽章。</summary>
        /// <typeparam name="T">與 RSA 或 ECC 驗章路徑相容的 key model 類型。</typeparam>
        /// <param name="data">原始簽章資料。</param>
        /// <param name="signature">要驗證的簽章 bytes。</param>
        /// <param name="alg">簽章演算法類型。</param>
        /// <param name="publicKey">含公鑰 PEM 的 model。</param>
        /// <param name="service">實際執行驗章的 service。</param>
        /// <returns>簽章有效時為 <see langword="true"/>，否則為 <see langword="false"/>。</returns>
        /// <exception cref="System.ArgumentNullException">必要輸入為 null。</exception>
        /// <exception cref="System.NotSupportedException">model 或演算法不支援驗章。</exception>
        public static bool VerifyWith<T>(this byte[] data, byte[] signature, CryptoAlgorithmType alg, T publicKey, ICryptoService service) where T : class => service.Verify(data, signature, alg, publicKey);
    }
}
