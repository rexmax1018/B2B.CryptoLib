using System;
using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// ECC 金鑰模型，包含 PEM 格式金鑰與所使用的曲線資訊。
    /// </summary>
    /// <remarks>
    /// <see cref="PublicKey"/> 與 <see cref="PrivateKey"/> 是 PEM 文字，不是檔案路徑。
    /// 私鑰含敏感資料，模型只應留在受保護的程序記憶體中，且不應寫入 log。
    /// </remarks>
    public class EccKeyModel
    {
        /// <summary>可供驗章使用的 PEM 公鑰內容。</summary>
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>可供簽章使用的 PEM 私鑰內容。</summary>
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>產生此 key pair 時使用的 <see cref="EccCurveType"/>。</summary>
        public EccCurveType Curve
        {
            get; set;
        }

        /// <summary>金鑰產生時間，以 UTC 表示；只供識別與維運，不參與密碼運算。</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
