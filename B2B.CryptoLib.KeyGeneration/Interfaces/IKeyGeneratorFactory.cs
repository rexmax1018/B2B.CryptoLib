using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    /// <summary>建立離線 AES、RSA 或 ECC key generator 的 factory。</summary>
    /// <remarks>factory 與其產生器屬於 key-generation tooling surface，不應部署到 runtime API。</remarks>
    public interface IKeyGeneratorFactory
    {
        /// <summary>依演算法與 model 型別建立 generator。</summary>
        /// <typeparam name="TModel">generator 產生的 model 類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <returns>相容的 <see cref="IKeyGenerator{TModel}"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與 model 不匹配。</exception>
        IKeyGenerator<TModel> Create<TModel>(CryptoAlgorithmType algorithm) where TModel : class;
    }
}
