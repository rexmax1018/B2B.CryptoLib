using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 提供加密、解密、簽章與驗章功能的泛型介面。
    /// </summary>
    /// <remarks>
    /// 實作支援的組合是 AES + <see cref="B2B.CryptoLib.Models.SymmetricKeyModel"/>、RSA +
    /// <see cref="B2B.CryptoLib.Models.RsaKeyModel"/>，以及 RSA/ECC + 對應 PEM 模型的簽章。
    /// 不相容的演算法與模型會拋出 <see cref="System.NotSupportedException"/>；此低階
    /// 介面不負責統一名稱、檔案輪替或文字編碼。
    /// </remarks>
    public interface ICryptoService
    {
        /// <summary>使用指定演算法與金鑰模型加密位元組。</summary>
        /// <typeparam name="TKeyModel">演算法所需的金鑰模型類型。</typeparam>
        /// <param name="data">要加密的位元組；不可為 null。</param>
        /// <param name="algorithm">要使用的演算法。</param>
        /// <param name="keyModel">包含演算法金鑰材料的模型。</param>
        /// <returns>加密後的位元組。</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="data"/> 為 null。</exception>
        /// <exception cref="System.NotSupportedException">演算法與金鑰模型不相容。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">金鑰內容或資料不符合底層密碼原語的要求。</exception>
        byte[] Encrypt<TKeyModel>(byte[]? data, CryptoAlgorithmType algorithm, TKeyModel keyModel) where TKeyModel : class;

        /// <summary>使用指定演算法與金鑰模型解密位元組。</summary>
        /// <typeparam name="TKeyModel">演算法所需的金鑰模型類型。</typeparam>
        /// <param name="encrypted">要解密的位元組；不可為 null。</param>
        /// <param name="algorithm">要使用的演算法。</param>
        /// <param name="keyModel">包含演算法金鑰材料的模型。</param>
        /// <returns>解密後的位元組。</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="encrypted"/> 為 null。</exception>
        /// <exception cref="System.NotSupportedException">演算法與金鑰模型不相容。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">密文、填充或金鑰無法解密。</exception>
        byte[] Decrypt<TKeyModel>(byte[]? encrypted, CryptoAlgorithmType algorithm, TKeyModel keyModel) where TKeyModel : class;

        /// <summary>使用私鑰對位元組產生數位簽章。</summary>
        /// <typeparam name="TKeyModel">RSA 或 ECC 金鑰模型類型。</typeparam>
        /// <param name="data">要簽章的位元組。</param>
        /// <param name="algorithm">RSA 或 ECC 簽章演算法類型。</param>
        /// <param name="privateKeyModel">含 PEM 私鑰的模型。</param>
        /// <returns>簽章位元組。</returns>
        /// <exception cref="System.ArgumentNullException">必要輸入為 null。</exception>
        /// <exception cref="System.NotSupportedException">模型或演算法不支援簽章。</exception>
        /// <exception cref="System.IO.InvalidDataException">PEM 私鑰無法解析。</exception>
        byte[] Sign<TKeyModel>(byte[] data, CryptoAlgorithmType algorithm, TKeyModel privateKeyModel) where TKeyModel : class;

        /// <summary>使用公鑰驗證數位簽章。</summary>
        /// <typeparam name="TKeyModel">RSA 或 ECC 金鑰模型類型。</typeparam>
        /// <param name="data">原始簽章資料。</param>
        /// <param name="signature">要驗證的簽章位元組。</param>
        /// <param name="algorithm">RSA 或 ECC 簽章演算法類型。</param>
        /// <param name="publicKeyModel">含 PEM 公鑰的模型。</param>
        /// <returns>簽章有效時為 true，無效時為 false。</returns>
        /// <exception cref="System.ArgumentNullException">必要輸入為 null。</exception>
        /// <exception cref="System.NotSupportedException">模型或演算法不支援驗章。</exception>
        /// <exception cref="System.IO.InvalidDataException">PEM 公鑰無法解析。</exception>
        bool Verify<TKeyModel>(byte[] data, byte[] signature, CryptoAlgorithmType algorithm, TKeyModel publicKeyModel) where TKeyModel : class;
    }
}
