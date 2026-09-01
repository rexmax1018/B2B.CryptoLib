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
    /// <remarks>
    /// 工廠只建立 AES、RSA、ECC 與其相應模型的組合，不會從呼叫資料猜測
    /// 演算法。產生器應只在離線工具或受控的金鑰作業環境使用。
    /// </remarks>
    public class KeyGeneratorFactory : IKeyGeneratorFactory
    {
        /// <summary>建立指定演算法及模型的離線產生器。</summary>
        /// <typeparam name="TModel">要產生的模型類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <returns>與 <paramref name="algorithm"/> 和 <typeparamref name="TModel"/> 相容的產生器。</returns>
        /// <exception cref="NotSupportedException">演算法與模型不匹配或尚未支援。</exception>
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
