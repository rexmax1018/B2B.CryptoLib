using System;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// RSA 金鑰模型，儲存 PEM 格式的公鑰與私鑰內容。
    /// </summary>
    public class RsaKeyModel
    {
        public string PublicKey { get; set; } = string.Empty;

        public string PrivateKey { get; set; } = string.Empty;

        public int KeySize
        {
            get; set;
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
