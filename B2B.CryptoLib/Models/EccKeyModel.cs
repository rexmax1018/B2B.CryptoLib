using System;
using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// ECC 金鑰模型，包含 PEM 格式金鑰與所使用的曲線資訊。
    /// </summary>
    public class EccKeyModel
    {
        public string PublicKey { get; set; } = string.Empty;

        public string PrivateKey { get; set; } = string.Empty;

        public EccCurveType Curve
        {
            get; set;
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
