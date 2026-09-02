using System;

namespace B2B.CryptoLib.KeyGeneration.Models
{
    /// <summary>
    /// 表示一組執行階段金鑰檔案的產生結果。
    /// </summary>
    /// <remarks>
    /// 結果指向 <c>update</c> 目錄的三個檔案：相容舊版的檔名所對應的 AES
    /// 材料、RSA 公開金鑰與 RSA 私密金鑰。路徑本身不是秘密，但其檔案內容
    /// 是；發布前應依 <see cref="B2B.CryptoLib.Services.KeyManagerService.StartAsync"/>
    /// 的生命週期規則處理。
    /// </remarks>
    public class KeySetGenerationResult
    {
        /// <summary>三個檔案共用的統一名稱。</summary>
        public string UnifiedName { get; set; } = string.Empty;
        /// <summary>update 目錄中的 AES 材料檔案路徑。</summary>
        public string AesKeyPath { get; set; } = string.Empty;
        /// <summary>update 目錄中的 RSA 公開 PEM 路徑。</summary>
        public string PublicKeyPath { get; set; } = string.Empty;
        /// <summary>update 目錄中的 RSA 私密 PEM 路徑。</summary>
        public string PrivateKeyPath { get; set; } = string.Empty;
        /// <summary>金鑰組產生完成時間，以 UTC 表示。</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>回傳包含名稱與 AES 材料路徑的單行摘要。</summary>
        /// <returns>供離線工具顯示的摘要文字；不包含金鑰位元組。</returns>
        public override string ToString() => $"[KEYSET] {UnifiedName} @ {AesKeyPath}";
    }
}
