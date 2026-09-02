using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;

namespace B2B.CryptoLib.Extensions
{
    /// <summary>
    /// 為 <see cref="ICryptoService"/> 提供 Encrypt、Decrypt、Sign 與 Verify 語法糖。
    /// </summary>
    /// <remarks>
    /// 擴充方法不建立服務、不保存金鑰，也不改變底層的演算法與金鑰模型配對規則。
    /// 它們只是把服務放在最後一個參數，讓既有的位元組陣列流程更易讀。
    /// </remarks>
    public static class CryptoExtensions
    {
        /// <summary>以指定服務及金鑰模型加密位元組。</summary>
        /// <typeparam name="T">與 <paramref name="alg"/> 相容的金鑰模型類型。</typeparam>
        /// <param name="data">要加密的資料；不可為 <see langword="null"/>。</param>
        /// <param name="alg">要使用的 <see cref="CryptoAlgorithmType"/>。</param>
        /// <param name="keyModel">演算法所需的金鑰模型。</param>
        /// <param name="service">實際執行加密的服務。</param>
        /// <returns>加密後的位元組。</returns>
        /// <exception cref="System.ArgumentNullException">資料為 null，或服務／金鑰模型不可用。</exception>
        /// <exception cref="System.NotSupportedException">演算法與模型不相容。</exception>
        public static byte[] EncryptWith<T>(this byte[] data, CryptoAlgorithmType alg, T keyModel, ICryptoService service) where T : class => service.Encrypt(data, alg, keyModel);

        /// <summary>以指定服務及金鑰模型解密位元組。</summary>
        /// <typeparam name="T">與 <paramref name="alg"/> 相容的金鑰模型類型。</typeparam>
        /// <param name="encrypted">要解密的資料；不可為 <see langword="null"/>。</param>
        /// <param name="alg">要使用的 <see cref="CryptoAlgorithmType"/>。</param>
        /// <param name="keyModel">演算法所需的金鑰模型。</param>
        /// <param name="service">實際執行解密的服務。</param>
        /// <returns>解密後的位元組。</returns>
        /// <exception cref="System.ArgumentNullException">資料為 null，或服務／金鑰模型不可用。</exception>
        /// <exception cref="System.NotSupportedException">演算法與模型不相容。</exception>
        public static byte[] DecryptWith<T>(this byte[] encrypted, CryptoAlgorithmType alg, T keyModel, ICryptoService service) where T : class => service.Decrypt(encrypted, alg, keyModel);

        /// <summary>以指定私鑰對位元組產生簽章。</summary>
        /// <typeparam name="T">與 RSA 或 ECC 簽章路徑相容的金鑰模型類型。</typeparam>
        /// <param name="data">要簽章的資料。</param>
        /// <param name="alg">簽章演算法類型。</param>
        /// <param name="privateKey">含私鑰 PEM 的模型。</param>
        /// <param name="service">實際執行簽章的服務。</param>
        /// <returns>產生的數位簽章位元組。</returns>
        /// <exception cref="System.ArgumentNullException">必要輸入為 null。</exception>
        /// <exception cref="System.NotSupportedException">模型或演算法不支援簽章。</exception>
        public static byte[] SignWith<T>(this byte[] data, CryptoAlgorithmType alg, T privateKey, ICryptoService service) where T : class => service.Sign(data, alg, privateKey);

        /// <summary>以指定公鑰驗證位元組的數位簽章。</summary>
        /// <typeparam name="T">與 RSA 或 ECC 驗章路徑相容的金鑰模型類型。</typeparam>
        /// <param name="data">原始簽章資料。</param>
        /// <param name="signature">要驗證的簽章位元組。</param>
        /// <param name="alg">簽章演算法類型。</param>
        /// <param name="publicKey">含公鑰 PEM 的模型。</param>
        /// <param name="service">實際執行驗章的服務。</param>
        /// <returns>簽章有效時為 <see langword="true"/>，否則為 <see langword="false"/>。</returns>
        /// <exception cref="System.ArgumentNullException">必要輸入為 null。</exception>
        /// <exception cref="System.NotSupportedException">模型或演算法不支援驗章。</exception>
        public static bool VerifyWith<T>(this byte[] data, byte[] signature, CryptoAlgorithmType alg, T publicKey, ICryptoService service) where T : class => service.Verify(data, signature, alg, publicKey);
    }
}
