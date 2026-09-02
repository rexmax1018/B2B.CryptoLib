using System;
using System.Text;

namespace B2B.CryptoLib.Extensions
{
    /// <summary>
    /// 提供 <see cref="byte"/> 陣列的 Base64、Hex 與 UTF-8 轉換擴充方法。
    /// </summary>
    /// <remarks>這些方法只做表示法轉換，不會清除或保護輸入位元組；呼叫端仍須妥善處理金鑰材料。</remarks>
    public static class ByteExtensions
    {
        /// <summary>將位元組編碼為標準 Base64 字串。</summary>
        /// <param name="data">要編碼的位元組；不可為 <see langword="null"/>。</param>
        /// <returns>包含填充的標準 Base64 表示。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> 為 <see langword="null"/>。</exception>
        public static string ToBase64(this byte[] data) => Convert.ToBase64String(data);

        /// <summary>將位元組轉成不含分隔符號的大寫十六進位字串。</summary>
        /// <param name="data">要轉換的位元組；不可為 <see langword="null"/>。</param>
        /// <returns>每個位元組對應兩個大寫十六進位字元的字串。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> 為 <see langword="null"/>。</exception>
        public static string ToHex(this byte[] data) => BitConverter.ToString(data).Replace("-", "");

        /// <summary>以 UTF-8 解碼位元組。</summary>
        /// <param name="data">要解碼的位元組；不可為 <see langword="null"/>。</param>
        /// <returns>解碼後的 .NET 字串。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> 為 <see langword="null"/>。</exception>
        public static string ToUtf8String(this byte[] data) => Encoding.UTF8.GetString(data);
    }
}
