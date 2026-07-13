using System;
using System.Globalization;
using System.Text;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供 Hex（十六進位）編碼與解碼工具方法。
    /// </summary>
    public static class HexUtils
    {
        public static string Encode(byte[] data, bool upperCase = true)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("輸入資料不可為空", nameof(data));

            var builder = new StringBuilder(data.Length * 2);
            var format = upperCase ? "X2" : "x2";

            foreach (var b in data)
                builder.Append(b.ToString(format));

            return builder.ToString();
        }

        public static byte[] Decode(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Hex 字串不可為空", nameof(hex));

            if (hex.Length % 2 != 0)
                throw new FormatException("Hex 字串長度必須為偶數");

            var bytes = new byte[hex.Length / 2];

            for (var i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = byte.Parse(hex.Substring(i, 2), NumberStyles.HexNumber);

            return bytes;
        }
    }
}
