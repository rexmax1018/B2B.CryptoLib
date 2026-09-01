using System;
using System.IO;
using System.Text;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;
using Newtonsoft.Json;

namespace B2B.CryptoLib.KeyLoaders
{
    /// <summary>
    /// RSA 金鑰載入器，將 JSON 格式金鑰資料還原為 RSA 模型。
    /// </summary>
    /// <remarks>
    /// loader 只還原 PEM 文字與 metadata，不會驗證 PEM 是否與另一個 key set 配對，
    /// 也不會將私鑰加密或清除。RSA/OAEP 與 legacy PKCS#1 v1.5 的用途由上層服務選擇。
    /// </remarks>
    public class RsaKeyLoader : IKeyLoader<RsaKeyModel>
    {
        /// <summary>從 UTF-8 JSON 檔案載入 RSA model。</summary>
        /// <param name="path">JSON 檔案路徑。</param>
        /// <returns>反序列化後的 <see cref="RsaKeyModel"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> 為 null。</exception>
        /// <exception cref="FileNotFoundException">檔案不存在。</exception>
        /// <exception cref="InvalidDataException">JSON 無法轉為 RSA model。</exception>
        public RsaKeyModel LoadFromFile(string path) => Deserialize(File.ReadAllText(path));

        /// <summary>從 UTF-8 JSON 文字載入 RSA model。</summary>
        /// <param name="content">要解析的 JSON 文字。</param>
        /// <returns>反序列化後的 <see cref="RsaKeyModel"/>。</returns>
        /// <exception cref="InvalidDataException">內容無法解析或結果為 null。</exception>
        public RsaKeyModel LoadFromString(string content) => Deserialize(content);

        /// <summary>從包含 UTF-8 JSON 的標準 Base64 字串載入 RSA model。</summary>
        /// <param name="base64">要解碼的 Base64 字串。</param>
        /// <returns>反序列化後的 <see cref="RsaKeyModel"/>。</returns>
        /// <exception cref="InvalidDataException">Base64 或解碼後 JSON 無效。</exception>
        public RsaKeyModel LoadFromBase64(string base64)
        {
            try
            {
                return Deserialize(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
            }
            catch (FormatException ex)
            {
                throw new InvalidDataException("無效的 Base64 字串格式", ex);
            }
        }

        /// <summary>讀取資料流剩餘內容並載入 RSA model。</summary>
        /// <param name="stream">可讀的 UTF-8 JSON stream。</param>
        /// <returns>反序列化後的 <see cref="RsaKeyModel"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> 為 null。</exception>
        /// <exception cref="InvalidDataException">內容無法解析。</exception>
        /// <remarks>方法會讀到 stream 結尾並關閉傳入 stream。</remarks>
        public RsaKeyModel LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return Deserialize(reader.ReadToEnd());
        }

        // Preserve PEM text exactly as persisted; CryptoService owns the
        // later algorithm-specific key parsing and validation.
        private static RsaKeyModel Deserialize(string json) => JsonConvert.DeserializeObject<RsaKeyModel>(json) ?? throw new InvalidDataException("無法解析 RSA 金鑰資料");
    }
}
