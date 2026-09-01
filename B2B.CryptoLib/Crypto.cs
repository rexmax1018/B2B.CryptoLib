using System;
using System.Threading;
using System.Threading.Tasks;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib
{
    /// <summary>
    /// 程序內的預設 <see cref="CryptoClient"/> 外觀層。
    /// </summary>
    /// <remarks>
    /// 這個外觀層只保存一個程序層級的執行階段執行脈絡；它不會讀取
    /// <c>appsettings.json</c>、建立預設的 <c>Keys</c> 目錄，也不會依名稱排序
    /// 自動挑選金鑰。需要多個金鑰根目錄或不同租戶時，請改用
    /// <see cref="CryptoClient.Create(CryptoOptions)"/> 建立隔離的用戶端。
    /// <para>
    /// 初始化與所有外觀層呼叫都共享同一個預設用戶端。初始化時會正規化
    /// <see cref="CryptoOptions.KeyManagerBasePath"/>；相同正規化設定可重複呼叫，
    /// 不同設定則會明確失敗。
    /// 正常使用時對已建立用戶端的外觀層讀取透過 volatile 參考，無須取得初始化鎖；
    /// 只有初始化與設定比較在鎖內執行。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Crypto.Initialize(new CryptoOptions
    /// {
    ///     KeyManagerBasePath = @"C:\CryptoKeys",
    ///     ActiveUnifiedName = "tenant-a"
    /// });
    /// var encrypted = Crypto.Encrypt("secret");
    /// var plainText = Crypto.Decrypt(encrypted);
    /// </code>
    /// </example>
    public static class Crypto
    {
        private static readonly object SyncRoot = new object();
        private static CryptoClient? _defaultClient;
        private static CryptoOptions? _defaultOptions;

        /// <summary>
        /// 設定程序內的預設用戶端。
        /// </summary>
        /// <param name="options">要使用的金鑰根目錄與可選的預設統一名稱。</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> 為 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException">路徑無效，或 <see cref="CryptoOptions.ActiveUnifiedName"/> 含有不允許的字元。</exception>
        /// <exception cref="InvalidOperationException">預設用戶端已用不同設定初始化。</exception>
        /// <remarks>
        /// 此方法具執行緒安全性。相同正規化設定的重複呼叫是冪等操作；初始化不會
        /// 掃描或消費 <c>update</c> 目錄。需要發布待處理金鑰時，請在初始化後明確
        /// 呼叫 <see cref="UpdateKeySetsAsync"/>。
        /// </remarks>
        public static void Initialize(CryptoOptions options)
        {
            var normalized = (options ?? throw new ArgumentNullException(nameof(options))).Normalize();

            lock (SyncRoot)
            {
                var existingClient = Volatile.Read(ref _defaultClient);

                if (existingClient is not null)
                {
                    var existingOptions = Volatile.Read(ref _defaultOptions);

                    if (existingOptions is not null && CryptoOptions.AreEquivalent(existingOptions, normalized))
                        return;

                    throw new InvalidOperationException("Crypto has already been initialized with a different configuration. Create an isolated CryptoClient for another key context.");
                }

                var client = new CryptoClient(normalized);
                // 先發布選項，再發布用戶端參考，讓每個免鎖讀取者
                // 都能觀察到完整且相互對應的預設執行脈絡。
                Volatile.Write(ref _defaultOptions, normalized);
                Volatile.Write(ref _defaultClient, client);
            }
        }

        /// <summary>
        /// 明確執行一次預設用戶端的 Update 金鑰組發布。
        /// </summary>
        /// <returns>在更新掃描與檔案發布完成後完成的工作。</returns>
        /// <exception cref="InvalidOperationException">尚未呼叫 <see cref="Initialize(CryptoOptions)"/>。</exception>
        /// <remarks>
        /// 這是明確的副作用操作；建立或取得預設用戶端不會自動觸發它。
        /// </remarks>
        public static Task UpdateKeySetsAsync() => GetDefaultClient().UpdateKeySetsAsync();

        /// <inheritdoc cref="CryptoClient.Encrypt(string?)" />
        /// <remarks>使用初始化時的 <see cref="CryptoOptions.ActiveUnifiedName"/>；未設定時會失敗。</remarks>
        public static string? Encrypt(string? plainText) => GetDefaultClient().Encrypt(plainText);

        /// <inheritdoc cref="CryptoClient.Encrypt(string?, string?)" />
        /// <remarks>明確指定的統一名稱不會被預設用戶端的啟用名稱覆寫。</remarks>
        public static string? Encrypt(string? plainText, string? unifiedName) => GetDefaultClient().Encrypt(plainText, unifiedName);

        /// <inheritdoc cref="CryptoClient.Decrypt(string?)" />
        /// <remarks>輸入為空時回傳 <see langword="null"/>；非空資料會依尾綴的統一名稱載入金鑰。</remarks>
        public static string? Decrypt(string? encryptedDataWithUnifiedName) => GetDefaultClient().Decrypt(encryptedDataWithUnifiedName);

        /// <summary>
        /// 僅驗證外層格式，不代表資料已通過訊息驗證或可使用目前金鑰解密。
        /// </summary>
        /// <param name="data">預期為 <c>Base64(payload).unifiedName</c> 的字串。</param>
        /// <returns>若外層含非空載荷、句點尾綴且載荷可被 Base64 解碼則為 <see langword="true"/>；否則為 <see langword="false"/>。</returns>
        /// <exception cref="InvalidOperationException">尚未初始化預設用戶端。</exception>
        /// <remarks>此方法不檢查 GCM 標籤、AAD、金鑰存在性或解密結果，因此不能當作驗證或授權檢查。</remarks>
        public static bool IsValidEncryptedFormat(string? data) => GetDefaultClient().IsValidEncryptedFormat(data);

        /// <inheritdoc cref="CryptoClient.GetUnifiedName(string?)" />
        /// <remarks>空輸入回傳 <see langword="null"/>；缺少有效尾綴時會拋出格式例外。</remarks>
        public static string? GetUnifiedName(string? encryptedDataWithUnifiedName) => GetDefaultClient().GetUnifiedName(encryptedDataWithUnifiedName);

        /// <inheritdoc cref="CryptoClient.GetUnifiedNameFromEncryptedData(string?)" />
        /// <remarks>此名稱是 <see cref="GetUnifiedName(string?)"/> 的相容別名。</remarks>
        public static string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName) => GetDefaultClient().GetUnifiedNameFromEncryptedData(encryptedDataWithUnifiedName);

        // 維持 internal，讓測試可以隔離程序層級狀態，避免增加正式環境呼叫端
        // 可能誤用的公開重設操作。
        internal static void ResetForTests()
        {
            lock (SyncRoot)
            {
                Volatile.Write(ref _defaultClient, null);
                Volatile.Write(ref _defaultOptions, null);
            }
        }

        private static CryptoClient GetDefaultClient()
        {
            return Volatile.Read(ref _defaultClient) ?? throw new InvalidOperationException("Crypto has not been initialized. Call Crypto.Initialize(...) before using it.");
        }
    }
}
