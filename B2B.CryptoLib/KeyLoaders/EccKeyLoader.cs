using System;
using System.IO;
using System.Text;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;
using Newtonsoft.Json;

namespace B2B.CryptoLib.KeyLoaders
{
    /// <summary>
    /// ECC 金鑰載入器，將 JSON 格式金鑰資料還原為 ECC 模型。
    /// </summary>
    /// <remarks>
    /// 載入器只還原 PEM 文字與曲線中繼資料，不會驗證曲線是否與 PEM 內容一致，
    /// 也不會保護或清除私鑰。實際簽章時由 <see cref="Services.CryptoService"/> 解析 PEM。
    /// </remarks>
    public class EccKeyLoader : IKeyLoader<EccKeyModel>
    {
    /// <summary>從 UTF-8 JSON 檔案載入 ECC 模型。</summary>
        /// <param name="path">JSON 檔案路徑。</param>
        /// <returns>反序列化後的 <see cref="EccKeyModel"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> 為 null。</exception>
        /// <exception cref="FileNotFoundException">檔案不存在。</exception>
    /// <exception cref="InvalidDataException">JSON 無法轉為 ECC 模型。</exception>
        public EccKeyModel LoadFromFile(string path) => Deserialize(File.ReadAllText(path));

    /// <summary>從 UTF-8 JSON 文字載入 ECC 模型。</summary>
        /// <param name="content">要解析的 JSON 文字。</param>
        /// <returns>反序列化後的 <see cref="EccKeyModel"/>。</returns>
        /// <exception cref="InvalidDataException">內容無法解析或結果為 null。</exception>
        public EccKeyModel LoadFromString(string content) => Deserialize(content);

    /// <summary>從包含 UTF-8 JSON 的標準 Base64 字串載入 ECC 模型。</summary>
        /// <param name="base64">要解碼的 Base64 字串。</param>
        /// <returns>反序列化後的 <see cref="EccKeyModel"/>。</returns>
        /// <exception cref="InvalidDataException">Base64 或解碼後 JSON 無效。</exception>
        public EccKeyModel LoadFromBase64(string base64)
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

    /// <summary>讀取資料流剩餘內容並載入 ECC 模型。</summary>
    /// <param name="stream">可讀的 UTF-8 JSON 資料流。</param>
        /// <returns>反序列化後的 <see cref="EccKeyModel"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> 為 null。</exception>
        /// <exception cref="InvalidDataException">內容無法解析。</exception>
    /// <remarks>方法會讀到資料流結尾並關閉傳入資料流。</remarks>
        public EccKeyModel LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return Deserialize(reader.ReadToEnd());
        }

        // 保持序列化的 PEM 與曲線中繼資料不變，讓既有簽章呼叫端
        // 繼續使用既定的金鑰表示法。
        private static EccKeyModel Deserialize(string json) => JsonConvert.DeserializeObject<EccKeyModel>(json) ?? throw new InvalidDataException("無法解析 ECC 金鑰資料");
    }
}
