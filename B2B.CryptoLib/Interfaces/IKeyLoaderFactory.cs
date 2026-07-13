using B2B.CryptoLib.Enums;
namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 金鑰載入器工廠介面。
    /// </summary>
    public interface IKeyLoaderFactory
    {
        IKeyLoader<TModel> Create<TModel>(CryptoAlgorithmType algorithm) where TModel : class;
    }
}
