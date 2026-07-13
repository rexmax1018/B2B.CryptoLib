using System;
using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.KeyGeneration.Models
{
    /// <summary>
    /// 表示一次離線金鑰產生與儲存作業的結果資訊。
    /// </summary>
    public class KeyGenerationResult
    {
        public CryptoAlgorithmType Algorithm { get; set; }
        public string KeyFileName { get; set; } = string.Empty;
        public string KeyFilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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

        public override string ToString() => $"[{Algorithm}] {KeyFileName} @ {KeyFilePath} (UTC {CreatedAt:yyyy-MM-dd HH:mm:ss})";
    }
}
