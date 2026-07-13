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
    public class AesKeyLoader : IKeyLoader<SymmetricKeyModel>
    {
        public SymmetricKeyModel LoadFromFile(string path) => Deserialize(File.ReadAllText(path));

        public SymmetricKeyModel LoadFromString(string content) => Deserialize(content);

        public SymmetricKeyModel LoadFromBase64(string base64) => Deserialize(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));

        public SymmetricKeyModel LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return Deserialize(reader.ReadToEnd());
        }

        private static SymmetricKeyModel Deserialize(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<SymmetricKeyModel>(json) ?? throw new InvalidDataException("JSON 內容無效：無法轉換為金鑰物件。");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("JSON 格式解析錯誤。", ex);
            }
        }
    }
}
