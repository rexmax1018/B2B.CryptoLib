using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    /// <summary>
    /// 僅供本機離線工具使用的金鑰產生與儲存服務。
    /// </summary>
    public interface IKeyGenerationService
    {
        TModel GenerateKeyOnly<TModel>(CryptoAlgorithmType algorithm) where TModel : class;

        KeyGenerationResult GenerateAndSaveKey<TModel>(CryptoAlgorithmType algorithm, string filePath = null) where TModel : class;
    }
}