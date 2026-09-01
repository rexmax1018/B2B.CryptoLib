using System;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// RSA 金鑰模型，儲存 PEM 格式的公鑰與私鑰內容。
    /// </summary>
    /// <remarks>
    /// PEM 內容的保存方式是 public API 契約的一部分：目前 RSA 資料加密使用 OAEP，
    /// 舊版 key-set 的 AES material 則使用獨立的 PKCS#1 v1.5 路徑。私鑰不得寫入
    /// log、版本控制或一般使用者可讀的輸出。
    /// </remarks>
    public class RsaKeyModel
    {
        /// <summary>可供 OAEP 加密或驗章使用的 PEM 公鑰內容。</summary>
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>可供 OAEP 解密、legacy material 解密或簽章使用的 PEM 私鑰內容。</summary>
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>RSA modulus 位元數；由 generator 記錄，載入器不會重新計算。</summary>
        public int KeySize
        {
            get; set;
        }

        /// <summary>金鑰產生時間，以 UTC 表示；只供識別與維運，不參與密碼運算。</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
