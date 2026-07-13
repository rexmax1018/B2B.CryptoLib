using System;

namespace B2B.CryptoLib.KeyGeneration.Models
{
    /// <summary>
    /// 表示一組 runtime 金鑰檔案的產生結果。
    /// </summary>
    public class KeySetGenerationResult
    {
        public string UnifiedName { get; set; } = string.Empty;
        public string AesKeyPath { get; set; } = string.Empty;
        public string PublicKeyPath { get; set; } = string.Empty;
        public string PrivateKeyPath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public override string ToString() => $"[KEYSET] {UnifiedName} @ {AesKeyPath}";
    }
}
