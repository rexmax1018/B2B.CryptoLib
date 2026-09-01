using B2B.CryptoLib.Enums;
namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 金鑰載入器工廠介面。
    /// </summary>
    /// <remarks>
    /// 工廠只接受目前支援的演算法與對應模型；例如 AES 必須配對
    /// <see cref="Models.SymmetricKeyModel"/>。不匹配組合應明確失敗，而不應
    /// 嘗試猜測輸入格式。
    /// </remarks>
    public interface IKeyLoaderFactory
    {
        /// <summary>建立指定演算法與模型的載入器。</summary>
        /// <typeparam name="TModel">載入器要產生的模型型別。</typeparam>
        /// <param name="algorithm">要處理的演算法類型。</param>
        /// <returns>與演算法及 <typeparamref name="TModel"/> 相容的載入器。</returns>
        /// <exception cref="System.NotSupportedException">演算法與模型類型不相容或尚未支援。</exception>
        IKeyLoader<TModel> Create<TModel>(CryptoAlgorithmType algorithm) where TModel : class;
    }
}
