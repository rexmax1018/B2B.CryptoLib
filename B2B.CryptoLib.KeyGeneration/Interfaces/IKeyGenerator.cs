using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    /// <summary>
    /// 離線金鑰產生器介面。實作不得部署到 WebAPI runtime。
    /// </summary>
    /// <remarks>
    /// generator 的 key material 會直接回傳給呼叫端或寫入檔案；介面不提供加密保存、
    /// secret zeroization 或備份策略。請在受控、離線的環境中使用。
    /// </remarks>
    public interface IKeyGenerator<TModel> where TModel : class
    {
        /// <summary>只產生並回傳一組 key model，不寫入檔案。</summary>
        /// <returns>新產生的 <typeparamref name="TModel"/>。</returns>
        /// <exception cref="System.InvalidOperationException">所需的 <see cref="B2B.CryptoLib.Config.CryptoConfig"/> 尚未載入。</exception>
        TModel GenerateKeyOnly();

        /// <summary>產生 key model 並以 generator 的格式寫入檔案。</summary>
        /// <param name="filePath">可選的檔名或路徑輸入；實作可能只採用檔名部分。</param>
        /// <returns>描述輸出檔案的 <see cref="KeyGenerationResult"/>。</returns>
        /// <exception cref="System.InvalidOperationException">所需的設定尚未載入。</exception>
        /// <exception cref="System.IO.IOException">輸出目錄或檔案無法寫入。</exception>
        KeyGenerationResult GenerateAndSaveKey(string? filePath = null);
    }
}
