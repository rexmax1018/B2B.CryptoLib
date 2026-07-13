using System;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// 描述同一統一名稱下 AES 與 RSA 金鑰檔案的位置。
    /// </summary>
    public class KeySetInfo
    {
        public string UnifiedName
        {
            get; set;
        }

        public string AesPath
        {
            get; set;
        }

        public string RsaPublicKeyPath
        {
            get; set;
        }

        public string RsaPrivateKeyPath
        {
            get; set;
        }

        public DateTime CreationTime
        {
            get; set;
        }

        public string[] GetAllPaths() => new[] { AesPath, RsaPublicKeyPath, RsaPrivateKeyPath };
    }
}
