using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;

namespace B2B.CryptoLib.Services
{
    /// <summary>
    /// 封裝 runtime 的金鑰載入器。
    /// </summary>
    public class CryptoKeyService : ICryptoKeyService
    {
        private readonly IKeyLoaderFactory _loaderFactory;

        public CryptoKeyService(IKeyLoaderFactory loaderFactory)
        {
            _loaderFactory = loaderFactory;
        }

        public TModel LoadFromFile<TModel>(CryptoAlgorithmType algorithm, string path) where TModel : class => _loaderFactory.Create<TModel>(algorithm).LoadFromFile(path);

        public TModel LoadFromBase64<TModel>(CryptoAlgorithmType algorithm, string base64) where TModel : class => _loaderFactory.Create<TModel>(algorithm).LoadFromBase64(base64);
    }
}
