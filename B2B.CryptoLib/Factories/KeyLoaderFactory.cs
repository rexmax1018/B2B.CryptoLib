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
    /// <remarks>
    /// 工廠不會從輸入內容推斷演算法；呼叫端必須同時提供正確的
    /// <see cref="CryptoAlgorithmType"/> 與模型泛型類型，避免把不同格式的
    /// 金鑰資料誤交給錯誤的載入器。
    /// </remarks>
    public class KeyLoaderFactory : IKeyLoaderFactory
    {
        /// <summary>建立與指定演算法及模型類型相容的金鑰載入器。</summary>
        /// <typeparam name="TModel">要載入的模型類型。</typeparam>
        /// <param name="algorithm">金鑰資料的演算法類型。</param>
        /// <returns>可載入 <typeparamref name="TModel"/> 的載入器。</returns>
        /// <exception cref="System.NotSupportedException">演算法與模型不匹配，或尚未有對應載入器。</exception>
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
