using System;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供 Base64 與 URL Safe Base64 編碼、解碼工具方法。
    /// </summary>
    /// <remarks>URL-safe 版本以 <c>-</c>/<c>_</c> 取代 <c>+</c>/<c>/</c>，並移除尾端填充；它不是訊息驗證。</remarks>
    public static class Base64Utils
    {
        /// <summary>將非空位元組編碼為標準 Base64。</summary>
        /// <param name="data">要編碼的資料。</param>
        /// <returns>含必要 <c>=</c> 填充的 Base64 字串。</returns>
        /// <exception cref="ArgumentException">資料為 null 或空陣列。</exception>
        public static string Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("輸入資料不可為空", nameof(data));

            return Convert.ToBase64String(data);
        }

        /// <summary>將非空標準 Base64 字串解碼為位元組。</summary>
        /// <param name="base64">要解碼的 Base64 字串。</param>
        /// <returns>解碼後的位元組。</returns>
        /// <exception cref="ArgumentException">字串為 null、空或只含空白。</exception>
        /// <exception cref="FormatException">字串不是有效 Base64。</exception>
        public static byte[] Decode(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Base64 字串不可為空", nameof(base64));

            return Convert.FromBase64String(base64);
        }

        /// <summary>將非空位元組編碼為不含填充的 URL-safe Base64。</summary>
        /// <param name="data">要編碼的資料。</param>
        /// <returns>使用 URL-safe 字元集且移除尾端填充的 Base64 字串。</returns>
        /// <exception cref="ArgumentException">資料為 null 或空陣列。</exception>
        public static string EncodeUrlSafe(byte[] data) => Encode(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        /// <summary>將 URL-safe Base64 字串還原並解碼為位元組。</summary>
        /// <param name="urlSafeBase64">不含或含尾端填充的 URL-safe Base64 字串。</param>
        /// <returns>解碼後的位元組。</returns>
        /// <exception cref="ArgumentException">字串為 null、空或只含空白。</exception>
        /// <exception cref="FormatException">字串不是有效 URL-safe Base64。</exception>
        public static byte[] DecodeUrlSafe(string urlSafeBase64)
        {
            if (string.IsNullOrWhiteSpace(urlSafeBase64))
                throw new ArgumentException("URL Safe Base64 字串不可為空", nameof(urlSafeBase64));

            var base64 = urlSafeBase64.Replace('-', '+').Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;

                case 3:
                    base64 += "=";
                    break;
            }

            return Decode(base64);
        }
    }
}
