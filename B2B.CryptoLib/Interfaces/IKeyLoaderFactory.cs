using B2B.CryptoLib.Enums;
namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 金鑰載入器工廠介面。
    /// </summary>
    /// <remarks>
    /// 工廠只接受目前支援的演算法與對應 model；例如 AES 必須配對
    /// <see cref="Models.SymmetricKeyModel"/>。不匹配組合應明確失敗，而不應
    /// 嘗試猜測輸入格式。
    /// </remarks>
    public interface IKeyLoaderFactory
    {
        /// <summary>建立指定演算法與 model 的 loader。</summary>
        /// <typeparam name="TModel">loader 要產生的 model 型別。</typeparam>
        /// <param name="algorithm">要處理的演算法類型。</param>
        /// <returns>與演算法及 <typeparamref name="TModel"/> 相容的 loader。</returns>
        /// <exception cref="System.NotSupportedException">演算法與 model 類型不相容或尚未支援。</exception>
        IKeyLoader<TModel> Create<TModel>(CryptoAlgorithmType algorithm) where TModel : class;
    }
}
