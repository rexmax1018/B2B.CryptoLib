using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    /// <summary>
    /// 離線金鑰產生器介面。實作不得部署到 WebAPI runtime。
    /// </summary>
    public interface IKeyGenerator<TModel> where TModel : class
    {
        TModel GenerateKeyOnly();

        KeyGenerationResult GenerateAndSaveKey(string? filePath = null);
    }
}
