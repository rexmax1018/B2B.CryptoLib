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
    /// <remarks>
    /// <see cref="CryptoConfig"/> 是舊版、process-wide 的設定入口，主要供離線
    /// KeyGeneration 與 KeyGenTool 使用。runtime 的 <see cref="CryptoClient"/> 與
    /// <see cref="Crypto"/> 不會自動讀取這個狀態；多個測試或租戶需要隔離設定時，
    /// 請使用 <see cref="Models.CryptoOptions"/> 建立 instance-local context。
    /// <para>
    /// 這個 static cache 沒有提供並行變更協定；宿主應在啟動或工具工作開始前
    /// 一次載入，並避免同時呼叫 <see cref="Load(string)"/> 與
    /// <see cref="Override(CryptoConfigModel?)"/>。
    /// </para>
    /// </remarks>
    public static class CryptoConfig
    {
        private static CryptoConfigModel? _cachedConfig;

        /// <summary>
            /// 取得目前已載入的 CryptoSuite 設定。
        /// </summary>
        /// <returns>最近一次由 <see cref="Load(string)"/> 或 <see cref="Override(CryptoConfigModel?)"/> 設定的模型。</returns>
        /// <exception cref="InvalidOperationException">目前尚未載入或覆寫設定。</exception>
        public static CryptoConfigModel Current => _cachedConfig ?? throw new InvalidOperationException("尚未載入 CryptoConfig，請先呼叫 Load 或 Override");

        /// <summary>
            /// 從 JSON 設定檔載入 CryptoSuite 區段。
            /// </summary>
        /// <param name="jsonPath">相對於應用程式 base directory 的檔案路徑；絕對路徑會先經 <see cref="Path.Combine(string, string)"/> 規則處理。</param>
        /// <exception cref="FileNotFoundException">設定檔不存在。</exception>
        /// <exception cref="InvalidDataException">檔案缺少非空的 <c>CryptoSuite</c> 節點，或該節點無法反序列化。</exception>
        /// <exception cref="JsonException">整份 JSON 不是有效 JSON。</exception>
        /// <remarks>
        /// 只載入 JSON 根節點下名為 <c>CryptoSuite</c> 的區段，未知欄位會被忽略。
        /// 呼叫會覆寫 process-wide 的 <see cref="Current"/>；不會建立金鑰目錄，
        /// 也不會觸發金鑰產生或發布。
        /// </remarks>
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
        /// <param name="model">要成為 <see cref="Current"/> 的設定；傳入 <see langword="null"/> 會清除目前 cache。</param>
        /// <remarks>
        /// 這是明確的全域狀態變更。傳入 null 後，下一次讀取
        /// <see cref="Current"/> 會拋出 <see cref="InvalidOperationException"/>。
        /// </remarks>
        public static void Override(CryptoConfigModel? model)
        {
            _cachedConfig = model;
        }

        /// <summary>
            /// 產生包含八碼英數字元的隨機金鑰檔名。
            /// </summary>
        /// <param name="extension">附加在八碼隨機名稱後的副檔名或字串；預設為 <c>.key</c>，方法不會驗證或正規化它。</param>
        /// <returns>由 cryptographically strong random bytes 選出的八碼英數字串加上 <paramref name="extension"/>。</returns>
        /// <remarks>
        /// 此名稱只用於降低離線工具檔名碰撞機率，不是金鑰材料，也不是安全授權 token。
        /// </remarks>
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

        /// <summary>
        /// 依目前設定的金鑰目錄、演算法目錄與檔名組合完整路徑。
        /// </summary>
        /// <param name="algorithm">例如 <c>AES</c>、<c>RSA</c> 或 <c>ECC</c> 的子目錄名稱。</param>
        /// <param name="fileName">要組合的檔名；此方法不替呼叫端驗證檔名安全性。</param>
        /// <returns>由 <see cref="CryptoConfigModel.KeyDirectory"/>、<paramref name="algorithm"/> 與 <paramref name="fileName"/> 組成的路徑。</returns>
        /// <exception cref="InvalidOperationException">尚未設定 <see cref="Current"/>。</exception>
        /// <exception cref="ArgumentNullException">任一路徑片段為 <see langword="null"/>。</exception>
        public static string GetKeyPath(string algorithm, string fileName) => Path.Combine(Current.KeyDirectory, algorithm, fileName);
    }
}
