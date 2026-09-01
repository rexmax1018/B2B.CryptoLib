using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    /// <summary>
    /// 產生可直接交給 runtime KeyManager 處理的完整 RSA/AES 金鑰組。
    /// </summary>
    public interface IKeySetGenerationService
    {
        KeySetGenerationResult GenerateAndSave(string? unifiedName = null);
    }
}
