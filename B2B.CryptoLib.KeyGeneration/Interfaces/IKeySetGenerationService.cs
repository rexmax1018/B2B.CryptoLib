using B2B.CryptoLib.KeyGeneration.Models;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    /// <summary>
    /// 產生可直接交給執行階段 KeyManager 處理的完整 RSA/AES 金鑰組。
    /// </summary>
    /// <remarks>
    /// 這個服務把三個檔案寫到 <c>CryptoConfig.Current.KeyDirectory\update</c>，
    /// 但不會自行呼叫執行階段的 <see cref="B2B.CryptoLib.Services.KeyManagerService.StartAsync"/>。
    /// 產生完成後仍須由部署流程顯式發布，並把 update 目錄當作含有秘密的來源保護。
    /// </remarks>
    public interface IKeySetGenerationService
    {
        /// <summary>產生同一統一名稱的 AES 材料、RSA 公開金鑰與 RSA 私密金鑰檔案。</summary>
        /// <param name="unifiedName">可選的英數字元、底線或連字號名稱；省略時產生八碼隨機名稱。</param>
        /// <returns>包含三個 update 檔案路徑的結果。</returns>
        /// <exception cref="System.ArgumentException">指定名稱含有不允許字元。</exception>
        /// <exception cref="System.InvalidOperationException">舊版 <see cref="B2B.CryptoLib.Config.CryptoConfig"/> 尚未載入。</exception>
        /// <exception cref="System.IO.IOException">檔案已存在、暫存檔無法寫入或發布失敗。</exception>
        /// <remarks>檔案會以公開金鑰、私密金鑰、AES 材料的順序完成發布；AES 檔案是執行階段的發現標記。</remarks>
        KeySetGenerationResult GenerateAndSave(string? unifiedName = null);
    }
}
