using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using B2B.CryptoLib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace B2B.CryptoLib.Config
{
    /// <summary>
    /// 負責載入、保存與提供 CryptoSuite 統一設定。
    /// </summary>
    public static class CryptoConfig
    {
        private static CryptoConfigModel? _cachedConfig;

        /// <summary>
        /// 取得目前已載入的 CryptoSuite 設定。
        /// </summary>
        public static CryptoConfigModel Current => _cachedConfig ?? throw new InvalidOperationException("尚未載入 CryptoConfig，請先呼叫 Load 或 Override");

        /// <summary>
        /// 從 JSON 設定檔載入 CryptoSuite 區段。
        /// </summary>
        public static void Load(string jsonPath = "appsettings.json")
        {
            jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, jsonPath);

            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"找不到設定檔：{jsonPath}");

            var section = JObject.Parse(File.ReadAllText(jsonPath, Encoding.UTF8))["CryptoSuite"]?.ToString();

            if (string.IsNullOrWhiteSpace(section))
                throw new InvalidDataException("設定檔中缺少 CryptoSuite 節點");

            _cachedConfig = JsonConvert.DeserializeObject<CryptoConfigModel>(section, new JsonSerializerSettings { Converters = { new StringEnumConverter() }, MissingMemberHandling = MissingMemberHandling.Ignore }) ?? throw new InvalidDataException("無法解析 CryptoSuite 設定");
        }

        /// <summary>
        /// 直接覆寫目前設定，供單元測試或宿主程式使用。
        /// </summary>
        public static void Override(CryptoConfigModel? model)
        {
            _cachedConfig = model;
        }

        /// <summary>
        /// 產生包含八碼英數字元的隨機金鑰檔名。
        /// </summary>
        public static string GenerateKeyFileName(string extension = ".key")
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int length = 8;

            var randomBytes = new byte[length];

            RandomNumberGenerator.Fill(randomBytes);

            var nameBuilder = new StringBuilder(length);

            for (var i = 0; i < length; i++)
                nameBuilder.Append(chars[randomBytes[i] % chars.Length]);

            return nameBuilder + extension;
        }

        public static string GetKeyPath(string algorithm, string fileName) => Path.Combine(Current.KeyDirectory, algorithm, fileName);
    }
}
