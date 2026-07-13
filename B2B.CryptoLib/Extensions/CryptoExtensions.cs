using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;

namespace B2B.CryptoLib.Extensions
{
    /// <summary>
    /// 為 <see cref="ICryptoService"/> 提供 Encrypt、Decrypt、Sign 與 Verify 語法糖。
    /// </summary>
    public static class CryptoExtensions
    {
        public static byte[] EncryptWith<T>(this byte[] data, CryptoAlgorithmType alg, T keyModel, ICryptoService service) where T : class => service.Encrypt(data, alg, keyModel);

        public static byte[] DecryptWith<T>(this byte[] encrypted, CryptoAlgorithmType alg, T keyModel, ICryptoService service) where T : class => service.Decrypt(encrypted, alg, keyModel);

        public static byte[] SignWith<T>(this byte[] data, CryptoAlgorithmType alg, T privateKey, ICryptoService service) where T : class => service.Sign(data, alg, privateKey);

        public static bool VerifyWith<T>(this byte[] data, byte[] signature, CryptoAlgorithmType alg, T publicKey, ICryptoService service) where T : class => service.Verify(data, signature, alg, publicKey);
    }
}
