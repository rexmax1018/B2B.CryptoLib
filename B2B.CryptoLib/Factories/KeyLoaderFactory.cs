using System;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.KeyLoaders;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib.Factories
{
    /// <summary>
    /// 依演算法類型與金鑰模型建立對應的金鑰載入器。
    /// </summary>
    public class KeyLoaderFactory : IKeyLoaderFactory
    {
        public IKeyLoader<TModel> Create<TModel>(CryptoAlgorithmType algorithm) where TModel : class
        {
            switch (algorithm)
            {
                case CryptoAlgorithmType.AES when typeof(TModel) == typeof(SymmetricKeyModel):
                    return (IKeyLoader<TModel>)new AesKeyLoader();

                case CryptoAlgorithmType.RSA when typeof(TModel) == typeof(RsaKeyModel):
                    return (IKeyLoader<TModel>)new RsaKeyLoader();

                case CryptoAlgorithmType.ECC when typeof(TModel) == typeof(EccKeyModel):
                    return (IKeyLoader<TModel>)new EccKeyLoader();

                default:
                    throw new NotSupportedException($"不支援的演算法或模型類型：{algorithm} → {typeof(TModel).Name}");
            }
        }
    }
}
