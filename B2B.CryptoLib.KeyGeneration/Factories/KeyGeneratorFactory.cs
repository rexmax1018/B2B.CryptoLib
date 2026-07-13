using System;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.KeyGenerators;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib.KeyGeneration.Factories
{
    /// <summary>
    /// 依演算法類型與金鑰模型建立離線金鑰產生器。
    /// </summary>
    public class KeyGeneratorFactory : IKeyGeneratorFactory
    {
        public IKeyGenerator<TModel> Create<TModel>(CryptoAlgorithmType algorithm) where TModel : class
        {
            switch (algorithm)
            {
                case CryptoAlgorithmType.AES when typeof(TModel) == typeof(SymmetricKeyModel):
                    return (IKeyGenerator<TModel>)new AesKeyGenerator();

                case CryptoAlgorithmType.RSA when typeof(TModel) == typeof(RsaKeyModel):
                    return (IKeyGenerator<TModel>)new RsaKeyGenerator();

                case CryptoAlgorithmType.ECC when typeof(TModel) == typeof(EccKeyModel):
                    return (IKeyGenerator<TModel>)new EccKeyGenerator();

                default:
                    throw new NotSupportedException($"不支援的演算法或模型類型：{algorithm} → {typeof(TModel).Name}");
            }
        }
    }
}