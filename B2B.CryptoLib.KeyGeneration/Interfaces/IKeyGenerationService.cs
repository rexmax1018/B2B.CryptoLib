using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    /// <summary>
    /// 僅供本機離線工具使用的金鑰產生與儲存服務。
    /// </summary>
    /// <remarks>
    /// 實作會呼叫對應 generator；<see cref="GenerateKeyOnly{TModel}(CryptoAlgorithmType)"/>
    /// 不寫檔，而 <see cref="GenerateAndSaveKey{TModel}(CryptoAlgorithmType, string?)"/>
    /// 會把 private key 與其他金鑰材料寫入由 <see cref="B2B.CryptoLib.Config.CryptoConfig"/>
    /// 決定的目錄。此介面不得註冊到 WebAPI runtime。
    /// </remarks>
    public interface IKeyGenerationService
    {
        /// <summary>只在記憶體中產生一組金鑰。</summary>
        /// <typeparam name="TModel">要產生的 key model 類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <returns>新產生的 key model。</returns>
        /// <exception cref="System.NotSupportedException">演算法與 model 不匹配。</exception>
        /// <remarks>方法不建立檔案；呼叫端仍必須保護回傳的秘密內容。</remarks>
        TModel GenerateKeyOnly<TModel>(CryptoAlgorithmType algorithm) where TModel : class;

        /// <summary>產生金鑰並以 JSON 寫入設定的演算法目錄。</summary>
        /// <typeparam name="TModel">要產生的 key model 類型。</typeparam>
        /// <param name="algorithm">要產生的演算法。</param>
        /// <param name="filePath">可選的檔名輸入；實作使用其檔名部分，未指定時會產生隨機名稱。</param>
        /// <returns>包含實際檔名、完整路徑與 UTC 建立時間的結果。</returns>
        /// <exception cref="System.NotSupportedException">演算法與 model 不匹配。</exception>
        /// <exception cref="System.InvalidOperationException">尚未載入 <see cref="B2B.CryptoLib.Config.CryptoConfig"/>。</exception>
        /// <exception cref="System.IO.IOException">目錄或檔案無法建立或寫入。</exception>
        KeyGenerationResult GenerateAndSaveKey<TModel>(CryptoAlgorithmType algorithm, string? filePath = null) where TModel : class;
    }
}
