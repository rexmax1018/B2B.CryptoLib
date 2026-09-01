using System;
using System.Text;
using Newtonsoft.Json;

namespace B2B.CryptoLib.Extensions
{
    /// <summary>
    /// 提供 Base64、Hex、文字編碼與 JSON 序列化的字串擴充方法。
    /// </summary>
    public static class StringExtensions
    {
        public static byte[] FromBase64(this string base64) => Convert.FromBase64String(base64);

        public static byte[] FromHex(this string hex)
        {
            if (hex.Length % 2 != 0)
                throw new FormatException("Hex string must have even length.");

            var result = new byte[hex.Length / 2];

            for (var i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return result;
        }

        public static byte[] ToBytes(this string text, Encoding? encoding = null) => (encoding ?? Encoding.UTF8).GetBytes(text);

        public static T FromJson<T>(this string json)
        {
            var result = JsonConvert.DeserializeObject<T>(json);

            if (result == null)
                throw new JsonException("反序列化失敗");

            return result;
        }

        public static string ToJson(this object obj, bool indented = false) => JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
    }
}
