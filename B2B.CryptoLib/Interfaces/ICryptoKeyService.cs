using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 提供 WebAPI runtime 所需的金鑰載入功能。
    /// 金鑰產生與儲存由 B2B.CryptoLib.KeyGeneration 的
    /// IKeyGenerationService 提供，且不得註冊於 WebAPI 容器。
    /// </summary>
    public interface ICryptoKeyService
    {
        TModel LoadFromFile<TModel>(CryptoAlgorithmType algorithm, string path) where TModel : class;

        TModel LoadFromBase64<TModel>(CryptoAlgorithmType algorithm, string base64) where TModel : class;
    }
}
