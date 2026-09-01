using System;

namespace B2B.CryptoLib.KeyGeneration.Models
{
    /// <summary>
    /// 表示一組 runtime 金鑰檔案的產生結果。
    /// </summary>
    /// <remarks>
    /// 結果指向 <c>update</c> 目錄的三個檔案：legacy-compatible 檔名的 AES
    /// material、RSA public key 與 RSA private key。路徑本身不是秘密，但其檔案內容
    /// 是；發布前應依 <see cref="B2B.CryptoLib.Services.KeyManagerService.StartAsync"/>
    /// 的生命週期規則處理。
    /// </remarks>
    public class KeySetGenerationResult
    {
        /// <summary>三個檔案共用的 unified name。</summary>
        public string UnifiedName { get; set; } = string.Empty;
        /// <summary>update 目錄中的 AES material 檔案路徑。</summary>
        public string AesKeyPath { get; set; } = string.Empty;
        /// <summary>update 目錄中的 RSA public PEM 路徑。</summary>
        public string PublicKeyPath { get; set; } = string.Empty;
        /// <summary>update 目錄中的 RSA private PEM 路徑。</summary>
        public string PrivateKeyPath { get; set; } = string.Empty;
        /// <summary>key set 產生完成時間，以 UTC 表示。</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>回傳包含名稱與 AES material 路徑的單行摘要。</summary>
        /// <returns>供離線工具顯示的摘要文字；不包含金鑰 bytes。</returns>
        public override string ToString() => $"[KEYSET] {UnifiedName} @ {AesKeyPath}";
    }
}
