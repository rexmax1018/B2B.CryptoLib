using System;
using System.Globalization;
using System.Text;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供 Hex（十六進位）編碼與解碼工具方法。
    /// </summary>
    /// <remarks>Hex 僅是可讀的位元組表示法，不提供加密、完整性或秘密保護。</remarks>
    public static class HexUtils
    {
        /// <summary>將非空位元組編碼為連續的十六進位字串。</summary>
        /// <param name="data">要編碼的資料。</param>
        /// <param name="upperCase">true 產生大寫 A-F；false 產生小寫 a-f。</param>
        /// <returns>每個位元組兩個字元的 Hex 字串。</returns>
        /// <exception cref="ArgumentException">資料為 null 或空陣列。</exception>
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

        /// <summary>將偶數長度 Hex 字串解碼為位元組。</summary>
        /// <param name="hex">每兩個字元代表一個位元組的字串。</param>
        /// <returns>解碼後的位元組。</returns>
        /// <exception cref="ArgumentException">字串為 null、空或只含空白。</exception>
        /// <exception cref="FormatException">長度為奇數或含非 Hex 字元。</exception>
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
