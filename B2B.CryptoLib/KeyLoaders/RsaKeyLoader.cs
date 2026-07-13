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
    public class RsaKeyLoader : IKeyLoader<RsaKeyModel>
    {
        public RsaKeyModel LoadFromFile(string path) => Deserialize(File.ReadAllText(path));

        public RsaKeyModel LoadFromString(string content) => Deserialize(content);

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

        public RsaKeyModel LoadFromStream(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return Deserialize(reader.ReadToEnd());
        }

        private static RsaKeyModel Deserialize(string json) => JsonConvert.DeserializeObject<RsaKeyModel>(json) ?? throw new InvalidDataException("無法解析 RSA 金鑰資料");
    }
}
