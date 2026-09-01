using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 提供加密、解密、簽章與驗章功能的泛型介面。
    /// </summary>
    public interface ICryptoService
    {
        byte[] Encrypt<TKeyModel>(byte[]? data, CryptoAlgorithmType algorithm, TKeyModel keyModel) where TKeyModel : class;

        byte[] Decrypt<TKeyModel>(byte[]? encrypted, CryptoAlgorithmType algorithm, TKeyModel keyModel) where TKeyModel : class;

        byte[] Sign<TKeyModel>(byte[] data, CryptoAlgorithmType algorithm, TKeyModel privateKeyModel) where TKeyModel : class;

        bool Verify<TKeyModel>(byte[] data, byte[] signature, CryptoAlgorithmType algorithm, TKeyModel publicKeyModel) where TKeyModel : class;
    }
}
