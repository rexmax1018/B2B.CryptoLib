using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;

namespace B2B.CryptoLib.Services
{
    /// <summary>
    /// 封裝執行階段的金鑰載入器。
    /// </summary>
    /// <remarks>
    /// 此服務只負責依演算法轉送到 <see cref="IKeyLoaderFactory"/>；它不產生金鑰、
    /// 不執行金鑰組輪替，也不會替呼叫端保護已載入的私密金鑰位元組。
    /// </remarks>
    public class CryptoKeyService : ICryptoKeyService
    {
        private readonly IKeyLoaderFactory _loaderFactory;

        /// <summary>建立使用指定載入器工廠的執行階段金鑰服務。</summary>
        /// <param name="loaderFactory">建立演算法相容載入器的工廠。</param>
        /// <remarks>工廠不可為 null，且應可在服務生命週期內使用；服務不會複製或釋放它。</remarks>
        public CryptoKeyService(IKeyLoaderFactory loaderFactory)
        {
            _loaderFactory = loaderFactory;
        }

        /// <summary>從檔案載入指定演算法的金鑰模型。</summary>
        /// <typeparam name="TModel">要還原的模型類型。</typeparam>
        /// <param name="algorithm">檔案內容的演算法。</param>
        /// <param name="path">序列化金鑰模型的檔案路徑。</param>
        /// <returns>載入的 <typeparamref name="TModel"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與模型不相容。</exception>
        /// <exception cref="System.IO.IOException">檔案無法讀取。</exception>
        /// <exception cref="System.IO.InvalidDataException">內容無法解析。</exception>
        public TModel LoadFromFile<TModel>(CryptoAlgorithmType algorithm, string path) where TModel : class => _loaderFactory.Create<TModel>(algorithm).LoadFromFile(path);

        /// <summary>從 Base64 序列化內容載入指定演算法的金鑰模型。</summary>
        /// <typeparam name="TModel">要還原的模型類型。</typeparam>
        /// <param name="algorithm">Base64 內容的演算法。</param>
        /// <param name="base64">包含 UTF-8 JSON 的 Base64 字串。</param>
        /// <returns>載入的 <typeparamref name="TModel"/>。</returns>
        /// <exception cref="System.NotSupportedException">演算法與模型不相容。</exception>
        /// <exception cref="System.FormatException">Base64 格式無效。</exception>
        /// <exception cref="System.IO.InvalidDataException">解碼後內容無法解析。</exception>
        public TModel LoadFromBase64<TModel>(CryptoAlgorithmType algorithm, string base64) where TModel : class => _loaderFactory.Create<TModel>(algorithm).LoadFromBase64(base64);
    }
}
