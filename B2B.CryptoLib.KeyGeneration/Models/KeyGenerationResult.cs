using System;
using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.KeyGeneration.Models
{
    /// <summary>
    /// 表示一次離線金鑰產生與儲存作業的結果資訊。
    /// </summary>
    /// <remarks>
    /// 結果只描述輸出位置與中繼資料，不包含金鑰位元組。<see cref="KeyFilePath"/>
    /// 可能指向含私密金鑰的檔案；不要直接將整個結果寫入公開記錄或回傳給不受信任的呼叫端。
    /// </remarks>
    public class KeyGenerationResult
    {
        /// <summary>實際產生的 <see cref="CryptoAlgorithmType"/>。</summary>
        public CryptoAlgorithmType Algorithm { get; set; }
        /// <summary>輸出檔案名稱，不含其目錄。</summary>
        public string KeyFileName { get; set; } = string.Empty;
        /// <summary>輸出檔案的完整路徑。</summary>
        public string KeyFilePath { get; set; } = string.Empty;
        /// <summary>產生作業完成時間，以 UTC 表示。</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>建立一個描述金鑰輸出的結果物件。</summary>
        /// <param name="algorithm">產生的演算法。</param>
        /// <param name="fileName">輸出檔名。</param>
        /// <param name="fullPath">輸出的完整路徑。</param>
        /// <returns>包含輸入中繼資料與目前 UTC 時間的新結果。</returns>
        /// <remarks>此輔助方法只記錄傳入的中繼資料；它不會額外驗證檔名或路徑。</remarks>
        public static KeyGenerationResult Create(CryptoAlgorithmType algorithm, string fileName, string fullPath)
        {
            return new KeyGenerationResult
            {
                Algorithm = algorithm,
                KeyFileName = fileName,
                KeyFilePath = fullPath,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>回傳適合離線工具輸出的單行摘要。</summary>
        /// <returns>包含演算法、檔名、路徑與 UTC 時間的文字。</returns>
        public override string ToString() => $"[{Algorithm}] {KeyFileName} @ {KeyFilePath} (UTC {CreatedAt:yyyy-MM-dd HH:mm:ss})";
    }
}
