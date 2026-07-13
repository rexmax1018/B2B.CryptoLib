using System;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供 Base64 與 URL Safe Base64 編碼、解碼工具方法。
    /// </summary>
    public static class Base64Utils
    {
        public static string Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("輸入資料不可為空", nameof(data));

            return Convert.ToBase64String(data);
        }

        public static byte[] Decode(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Base64 字串不可為空", nameof(base64));

            return Convert.FromBase64String(base64);
        }

        public static string EncodeUrlSafe(byte[] data) => Encode(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

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
