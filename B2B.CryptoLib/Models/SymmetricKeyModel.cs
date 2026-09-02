using System;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// 表示對稱式加密使用的金鑰與初始向量（IV）。
    /// </summary>
    /// <remarks>
    /// <see cref="Key"/> 與 <see cref="IV"/> 是原始位元組；目前 GCM v2 只使用 Key
    /// 並在每次加密產生 nonce（隨機數），舊版 AES-CBC 則使用這裡的 IV。
    /// 兩個陣列都含敏感材料，不應序列化到記錄或提交至版本控制。
    /// </remarks>
    public class SymmetricKeyModel
    {
        /// <summary>供 AES 使用的 16、24 或 32 個位元組金鑰。</summary>
        public byte[] Key { get; set; } = Array.Empty<byte>();

        /// <summary>舊版 AES-CBC 使用的初始化向量；一般 GCM v2 加密不使用此欄位。</summary>
        public byte[] IV { get; set; } = Array.Empty<byte>();
    }
}
