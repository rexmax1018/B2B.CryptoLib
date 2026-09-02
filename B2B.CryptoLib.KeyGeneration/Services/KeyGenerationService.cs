using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Services
{
    /// <summary>
    /// 封裝離線產生器；此服務只由 KeyGenTool 註冊與使用。
    /// </summary>
    /// <remarks>
    /// 服務本身不保存金鑰材料；每次呼叫都透過
    /// <see cref="IKeyGeneratorFactory"/> 建立或取得與模型相容的產生器。
    /// 它屬於離線工具邊界，不應註冊進 WebAPI 執行階段。
    /// </remarks>
    public class KeyGenerationService : IKeyGenerationService
    {
        private readonly IKeyGeneratorFactory _generatorFactory;

        /// <summary>建立使用指定產生器工廠的離線服務。</summary>
        /// <param name="generatorFactory">建立 AES/RSA/ECC 產生器的工廠。</param>
        /// <remarks>工廠應在服務的整個生命週期內有效；服務不會釋放它。</remarks>
        public KeyGenerationService(IKeyGeneratorFactory generatorFactory)
        {
            _generatorFactory = generatorFactory;
        }

        /// <summary>只在記憶體中產生指定類型的金鑰模型。</summary>
        /// <typeparam name="TModel">要產生的模型類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <returns>由相容產生器產生的 <typeparamref name="TModel"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與模型不匹配。</exception>
        public TModel GenerateKeyOnly<TModel>(CryptoAlgorithmType algorithm) where TModel : class
        {
            return _generatorFactory.Create<TModel>(algorithm).GenerateKeyOnly();
        }

        /// <summary>產生指定類型的金鑰模型並將其保存為 JSON。</summary>
        /// <typeparam name="TModel">要產生的模型類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <param name="filePath">可選的輸出檔名或路徑。</param>
        /// <returns>描述產出檔案的 <see cref="KeyGenerationResult"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與模型不匹配。</exception>
        /// <exception cref="System.IO.IOException">輸出無法寫入。</exception>
        public KeyGenerationResult GenerateAndSaveKey<TModel>(CryptoAlgorithmType algorithm, string? filePath = null) where TModel : class
        {
            return _generatorFactory.Create<TModel>(algorithm).GenerateAndSaveKey(filePath);
        }
    }
}
