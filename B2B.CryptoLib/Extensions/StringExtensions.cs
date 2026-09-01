using System;
using System.Text;
using Newtonsoft.Json;

namespace B2B.CryptoLib.Extensions
{
    /// <summary>
    /// 提供 Base64、Hex、文字編碼與 JSON 序列化的字串擴充方法。
    /// </summary>
    /// <remarks>這些方法使用標準 .NET／Newtonsoft 表示法，不會自動套用 CryptoSuite 的金鑰組或封裝格式規則。</remarks>
    public static class StringExtensions
    {
        /// <summary>將標準 Base64 字串解碼為位元組。</summary>
        /// <param name="base64">包含有效 Base64 字元與填充的字串。</param>
        /// <returns>解碼後的位元組。</returns>
        /// <exception cref="FormatException">字串不是有效 Base64。</exception>
        /// <exception cref="ArgumentNullException"><paramref name="base64"/> 為 <see langword="null"/>。</exception>
        public static byte[] FromBase64(this string base64) => Convert.FromBase64String(base64);

        /// <summary>將偶數長度的十六進位字串解碼為位元組。</summary>
        /// <param name="hex">每兩個字元表示一個位元組的十六進位字串。</param>
        /// <returns>解碼後的位元組；空字串會回傳空陣列。</returns>
        /// <exception cref="FormatException">長度為奇數或包含非十六進位字元。</exception>
        public static byte[] FromHex(this string hex)
        {
            if (hex.Length % 2 != 0)
                throw new FormatException("Hex string must have even length.");

            var result = new byte[hex.Length / 2];

            for (var i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return result;
        }

        /// <summary>以指定編碼將文字轉為位元組。</summary>
        /// <param name="text">要編碼的文字。</param>
        /// <param name="encoding">使用的編碼；省略時為 UTF-8。</param>
        /// <returns>編碼後的位元組。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> 為 <see langword="null"/>。</exception>
        public static byte[] ToBytes(this string text, Encoding? encoding = null) => (encoding ?? Encoding.UTF8).GetBytes(text);

        /// <summary>將 JSON 文字反序列化為指定型別。</summary>
        /// <typeparam name="T">預期的結果型別。</typeparam>
        /// <param name="json">要解析的 JSON 文字。</param>
        /// <returns>反序列化後的非 null 物件。</returns>
        /// <exception cref="JsonException">JSON 無法解析，或結果為 null。</exception>
        public static T FromJson<T>(this string json)
        {
            var result = JsonConvert.DeserializeObject<T>(json);

            if (result == null)
                throw new JsonException("反序列化失敗");

            return result;
        }

        /// <summary>將物件序列化為 JSON。</summary>
        /// <param name="obj">要序列化的物件；null 會依 Newtonsoft.Json 規則產生 <c>null</c>。</param>
        /// <param name="indented">是否使用縮排格式。</param>
        /// <returns>序列化後的 JSON 文字。</returns>
        public static string ToJson(this object obj, bool indented = false) => JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
    }
}
