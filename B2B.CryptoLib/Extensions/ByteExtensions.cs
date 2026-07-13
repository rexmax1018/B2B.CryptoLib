using System;
using System.Text;

namespace B2B.CryptoLib.Extensions
{
    /// <summary>
    /// 提供 <see cref="byte"/> 陣列的 Base64、Hex 與 UTF-8 轉換擴充方法。
    /// </summary>
    public static class ByteExtensions
    {
        public static string ToBase64(this byte[] data) => Convert.ToBase64String(data);

        public static string ToHex(this byte[] data) => BitConverter.ToString(data).Replace("-", "");

        public static string ToUtf8String(this byte[] data) => Encoding.UTF8.GetString(data);
    }
}
