using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Services
{
    /// <summary>
    /// 封裝離線產生器；此服務只由 KeyGenTool 註冊與使用。
    /// </summary>
    public class KeyGenerationService : IKeyGenerationService
    {
        private readonly IKeyGeneratorFactory _generatorFactory;

        public KeyGenerationService(IKeyGeneratorFactory generatorFactory)
        {
            _generatorFactory = generatorFactory;
        }

        public TModel GenerateKeyOnly<TModel>(CryptoAlgorithmType algorithm) where TModel : class
        {
            return _generatorFactory.Create<TModel>(algorithm).GenerateKeyOnly();
        }

        public KeyGenerationResult GenerateAndSaveKey<TModel>(CryptoAlgorithmType algorithm, string filePath = null) where TModel : class
        {
            return _generatorFactory.Create<TModel>(algorithm).GenerateAndSaveKey(filePath);
        }
    }
}
