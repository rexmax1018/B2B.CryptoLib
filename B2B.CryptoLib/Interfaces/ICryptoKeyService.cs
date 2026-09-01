using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 提供 WebAPI runtime 所需的金鑰載入功能。
    /// 金鑰產生與儲存由 B2B.CryptoLib.KeyGeneration 的
    /// IKeyGenerationService 提供，且不得註冊於 WebAPI 容器。
    /// </summary>
    /// <remarks>
    /// 這個介面只將序列化的 key model 載入記憶體，不產生金鑰、不發布 key set，
    /// 也不會替呼叫端保護或清除私鑰內容。請把它與離線的
    /// <c>B2B.CryptoLib.KeyGeneration.Interfaces.IKeyGenerationService</c> 分開部署。
    /// </remarks>
    public interface ICryptoKeyService
    {
        /// <summary>從檔案載入指定演算法的 key model。</summary>
        /// <typeparam name="TModel">要還原的 key model 型別。</typeparam>
        /// <param name="algorithm">檔案內容所對應的演算法。</param>
        /// <param name="path">包含序列化 key model 的檔案路徑。</param>
        /// <returns>反序列化後的 key model。</returns>
        /// <exception cref="System.ArgumentException">路徑或資料不合法。</exception>
        /// <exception cref="System.IO.FileNotFoundException">檔案不存在。</exception>
        /// <exception cref="System.IO.InvalidDataException">內容無法還原為指定 model。</exception>
        TModel LoadFromFile<TModel>(CryptoAlgorithmType algorithm, string path) where TModel : class;

        /// <summary>從 UTF-8 JSON 的 Base64 表示載入指定演算法的 key model。</summary>
        /// <typeparam name="TModel">要還原的 key model 型別。</typeparam>
        /// <param name="algorithm">Base64 內容所對應的演算法。</param>
        /// <param name="base64">包含 JSON bytes 的標準 Base64 字串。</param>
        /// <returns>反序列化後的 key model。</returns>
        /// <exception cref="System.ArgumentException">Base64 或資料內容不合法。</exception>
        /// <exception cref="System.IO.InvalidDataException">內容無法還原為指定 model。</exception>
        TModel LoadFromBase64<TModel>(CryptoAlgorithmType algorithm, string base64) where TModel : class;
    }
}
