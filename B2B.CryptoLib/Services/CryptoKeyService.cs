using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;

namespace B2B.CryptoLib.Services
{
    /// <summary>
    /// 封裝 runtime 的金鑰載入器。
    /// </summary>
    /// <remarks>
    /// 此服務只負責依演算法轉送到 <see cref="IKeyLoaderFactory"/>；它不產生金鑰、
    /// 不執行 key-set rotation，也不會替呼叫端保護已載入的 private key bytes。
    /// </remarks>
    public class CryptoKeyService : ICryptoKeyService
    {
        private readonly IKeyLoaderFactory _loaderFactory;

        /// <summary>建立使用指定 loader factory 的 runtime key service。</summary>
        /// <param name="loaderFactory">建立演算法相容 loader 的 factory。</param>
        /// <remarks>factory 應為 non-null 且可在 service 生命週期內使用；service 不會複製或 dispose 它。</remarks>
        public CryptoKeyService(IKeyLoaderFactory loaderFactory)
        {
            _loaderFactory = loaderFactory;
        }

        /// <summary>從檔案載入指定演算法的 key model。</summary>
        /// <typeparam name="TModel">要還原的 model 類型。</typeparam>
        /// <param name="algorithm">檔案內容的演算法。</param>
        /// <param name="path">序列化 key model 的檔案路徑。</param>
        /// <returns>載入的 <typeparamref name="TModel"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與 model 不相容。</exception>
        /// <exception cref="System.IO.IOException">檔案無法讀取。</exception>
        /// <exception cref="System.IO.InvalidDataException">內容無法解析。</exception>
        public TModel LoadFromFile<TModel>(CryptoAlgorithmType algorithm, string path) where TModel : class => _loaderFactory.Create<TModel>(algorithm).LoadFromFile(path);

        /// <summary>從 Base64 序列化內容載入指定演算法的 key model。</summary>
        /// <typeparam name="TModel">要還原的 model 類型。</typeparam>
        /// <param name="algorithm">Base64 內容的演算法。</param>
        /// <param name="base64">包含 UTF-8 JSON 的 Base64 字串。</param>
        /// <returns>載入的 <typeparamref name="TModel"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與 model 不相容。</exception>
        /// <exception cref="System.FormatException">Base64 格式無效。</exception>
        /// <exception cref="System.IO.InvalidDataException">解碼後內容無法解析。</exception>
        public TModel LoadFromBase64<TModel>(CryptoAlgorithmType algorithm, string base64) where TModel : class => _loaderFactory.Create<TModel>(algorithm).LoadFromBase64(base64);
    }
}
