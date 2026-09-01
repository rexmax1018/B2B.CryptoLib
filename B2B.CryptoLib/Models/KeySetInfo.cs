using System;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// 描述同一統一名稱下 AES 與 RSA 金鑰檔案的位置。
    /// </summary>
    public class KeySetInfo
    {
        public string UnifiedName { get; set; } = null!;

        public string AesPath { get; set; } = null!;

        public string RsaPublicKeyPath { get; set; } = null!;

        public string RsaPrivateKeyPath { get; set; } = null!;

        public DateTime CreationTime
        {
            get; set;
        }

        /// <summary>
        /// 指出 .der 金鑰組是否採用舊版 PKCS#1 v1.5 與句點分隔的 AES 內容格式。
        /// </summary>
        public bool UsesLegacyMaterial
        {
            get; set;
        }

        public string[] GetAllPaths() => new[] { AesPath, RsaPublicKeyPath, RsaPrivateKeyPath };
    }
}
