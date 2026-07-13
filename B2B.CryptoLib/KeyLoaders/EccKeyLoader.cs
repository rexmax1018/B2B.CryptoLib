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
    public class EccKeyLoader : IKeyLoader<EccKeyModel>
    {
        public EccKeyModel LoadFromFile(string path) => Deserialize(File.ReadAllText(path));

        public EccKeyModel LoadFromString(string content) => Deserialize(content);

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

        public EccKeyModel LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return Deserialize(reader.ReadToEnd());
        }

        private static EccKeyModel Deserialize(string json) => JsonConvert.DeserializeObject<EccKeyModel>(json) ?? throw new InvalidDataException("無法解析 ECC 金鑰資料");
    }
}
