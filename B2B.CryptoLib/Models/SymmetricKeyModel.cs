using System;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// 表示對稱式加密使用的金鑰與初始向量（IV）。
    /// </summary>
    public class SymmetricKeyModel
    {
        public byte[] Key { get; set; } = Array.Empty<byte>();

        public byte[] IV { get; set; } = Array.Empty<byte>();
    }
}
