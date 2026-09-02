using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    /// <summary>建立離線 AES、RSA 或 ECC 金鑰產生器的工廠。</summary>
    /// <remarks>工廠與其產生器屬於金鑰產生工具介面範圍，不應部署到執行階段 API。</remarks>
    public interface IKeyGeneratorFactory
    {
        /// <summary>依演算法與模型型別建立產生器。</summary>
        /// <typeparam name="TModel">產生器產生的模型類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <returns>相容的 <see cref="IKeyGenerator{TModel}"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與模型不匹配。</exception>
        IKeyGenerator<TModel> Create<TModel>(CryptoAlgorithmType algorithm) where TModel : class;
    }
}
