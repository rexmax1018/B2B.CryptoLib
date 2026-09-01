using System;
using System.IO;
using System.Text;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;
using Newtonsoft.Json;

namespace B2B.CryptoLib.KeyLoaders
{
    /// <summary>
    /// AES 金鑰載入器，支援檔案、字串、Base64 與資料流。
    /// </summary>
    /// <remarks>
    /// 載入器預期 UTF-8 JSON，其欄位可還原為 <see cref="SymmetricKeyModel"/> 的
    /// Key 與 IV。它只解析資料，不驗證金鑰長度；長度檢查會在實際密碼運算時發生。
    /// </remarks>
    public class AesKeyLoader : IKeyLoader<SymmetricKeyModel>
    {
    /// <summary>從 UTF-8 JSON 檔案載入 AES 模型。</summary>
        /// <param name="path">JSON 檔案路徑。</param>
    /// <returns>反序列化後的 <see cref="SymmetricKeyModel"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> 為 null。</exception>
        /// <exception cref="FileNotFoundException">檔案不存在。</exception>
    /// <exception cref="InvalidDataException">JSON 無法轉為 AES 模型。</exception>
        public SymmetricKeyModel LoadFromFile(string path) => Deserialize(File.ReadAllText(path));

    /// <summary>從 UTF-8 JSON 文字載入 AES 模型。</summary>
        /// <param name="content">要解析的 JSON 文字。</param>
        /// <returns>反序列化後的 <see cref="SymmetricKeyModel"/>。</returns>
        /// <exception cref="InvalidDataException">內容為空、JSON 無效或結果為 null。</exception>
        public SymmetricKeyModel LoadFromString(string content) => Deserialize(content);

    /// <summary>從包含 UTF-8 JSON 的標準 Base64 字串載入 AES 模型。</summary>
        /// <param name="base64">要解碼的 Base64 字串。</param>
        /// <returns>反序列化後的 <see cref="SymmetricKeyModel"/>。</returns>
        /// <exception cref="FormatException">Base64 格式無效。</exception>
        /// <exception cref="InvalidDataException">解碼後 JSON 無法解析。</exception>
        public SymmetricKeyModel LoadFromBase64(string base64) => Deserialize(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));

    /// <summary>讀取資料流剩餘內容並載入 AES 模型。</summary>
    /// <param name="stream">可讀的 UTF-8 JSON 資料流。</param>
        /// <returns>反序列化後的 <see cref="SymmetricKeyModel"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> 為 null。</exception>
        /// <exception cref="InvalidDataException">內容無法解析。</exception>
    /// <remarks>方法會讀到資料流結尾，並因 <see cref="StreamReader"/> 的 using 區塊而關閉傳入資料流。</remarks>
        public SymmetricKeyModel LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return Deserialize(reader.ReadToEnd());
        }

        private static SymmetricKeyModel Deserialize(string json)
        {
            try
            {
                // JSON 載入只負責還原已保存的模型形狀；金鑰長度檢查
                // 屬於密碼原語邊界的責任。
                return JsonConvert.DeserializeObject<SymmetricKeyModel>(json) ?? throw new InvalidDataException("JSON 內容無效：無法轉換為金鑰物件。");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("JSON 格式解析錯誤。", ex);
            }
        }
    }
}
