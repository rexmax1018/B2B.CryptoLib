using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Services
{
    /// <summary>
    /// 封裝離線產生器；此服務只由 KeyGenTool 註冊與使用。
    /// </summary>
    /// <remarks>
    /// service 本身不保存 key material；每次呼叫都透過
    /// <see cref="IKeyGeneratorFactory"/> 建立或取得與 model 相容的 generator。
    /// 它屬於離線工具邊界，不應註冊進 WebAPI runtime。
    /// </remarks>
    public class KeyGenerationService : IKeyGenerationService
    {
        private readonly IKeyGeneratorFactory _generatorFactory;

        /// <summary>建立使用指定 generator factory 的離線服務。</summary>
        /// <param name="generatorFactory">建立 AES/RSA/ECC generator 的 factory。</param>
        /// <remarks>factory 應在 service 的整個生命週期內有效；service 不會 dispose 它。</remarks>
        public KeyGenerationService(IKeyGeneratorFactory generatorFactory)
        {
            _generatorFactory = generatorFactory;
        }

        /// <summary>只在記憶體中產生指定類型的 key model。</summary>
        /// <typeparam name="TModel">要產生的 model 類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <returns>由相容 generator 產生的 <typeparamref name="TModel"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與 model 不匹配。</exception>
        public TModel GenerateKeyOnly<TModel>(CryptoAlgorithmType algorithm) where TModel : class
        {
            return _generatorFactory.Create<TModel>(algorithm).GenerateKeyOnly();
        }

        /// <summary>產生指定類型的 key model 並將其保存為 JSON。</summary>
        /// <typeparam name="TModel">要產生的 model 類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <param name="filePath">可選的輸出檔名或路徑。</param>
        /// <returns>描述產出檔案的 <see cref="KeyGenerationResult"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與 model 不匹配。</exception>
        /// <exception cref="System.IO.IOException">輸出無法寫入。</exception>
        public KeyGenerationResult GenerateAndSaveKey<TModel>(CryptoAlgorithmType algorithm, string? filePath = null) where TModel : class
        {
            return _generatorFactory.Create<TModel>(algorithm).GenerateAndSaveKey(filePath);
        }
    }
}
