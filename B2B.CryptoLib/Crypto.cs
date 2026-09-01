using System;
using System.Threading;
using System.Threading.Tasks;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib
{
    /// <summary>
    /// Process 內的 default <see cref="CryptoClient"/> facade。
    /// </summary>
    /// <remarks>
    /// 這個 facade 只保存一個 process-wide runtime context；它不會讀取
    /// <c>appsettings.json</c>、建立預設的 <c>Keys</c> 目錄，也不會依名稱排序
    /// 自動挑選金鑰。需要多個金鑰根目錄或不同租戶時，請改用
    /// <see cref="CryptoClient.Create(CryptoOptions)"/> 建立隔離的 client。
    /// <para>
    /// 初始化與所有 facade 呼叫都共享同一個 default client。初始化時會正規化
    /// <see cref="CryptoOptions.KeyManagerBasePath"/>；相同正規化設定可重複呼叫，
    /// 不同設定則會明確失敗。
    /// 正常使用時對已建立 client 的 facade 讀取透過 volatile reference，無須取得初始化鎖；
    /// 只有初始化與設定比較在 lock 內執行。
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
        /// 設定 process 內的 default client。
        /// </summary>
        /// <param name="options">要使用的金鑰根目錄與可選的預設 unified name。</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> 為 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException">路徑無效，或 <see cref="CryptoOptions.ActiveUnifiedName"/> 含有不允許的字元。</exception>
        /// <exception cref="InvalidOperationException">default client 已用不同設定初始化。</exception>
        /// <remarks>
        /// 此方法是 thread-safe。相同正規化設定的重複呼叫是冪等操作；初始化不會
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
                // Publish options before the client reference so every lock-free
                // reader observes a complete, matching default context.
                Volatile.Write(ref _defaultOptions, normalized);
                Volatile.Write(ref _defaultClient, client);
            }
        }

        /// <summary>
        /// 明確執行一次 default client 的 Update 金鑰組發布。
        /// </summary>
        /// <returns>在更新掃描與檔案發布完成後完成的工作。</returns>
        /// <exception cref="InvalidOperationException">尚未呼叫 <see cref="Initialize(CryptoOptions)"/>。</exception>
        /// <remarks>
        /// 這是顯式副作用操作；建立或取得 default client 不會自動觸發它。
        /// </remarks>
        public static Task UpdateKeySetsAsync() => GetDefaultClient().UpdateKeySetsAsync();

        /// <inheritdoc cref="CryptoClient.Encrypt(string?)" />
        /// <remarks>使用初始化時的 <see cref="CryptoOptions.ActiveUnifiedName"/>；未設定時會失敗。</remarks>
        public static string? Encrypt(string? plainText) => GetDefaultClient().Encrypt(plainText);

        /// <inheritdoc cref="CryptoClient.Encrypt(string?, string?)" />
        /// <remarks>明確指定的 unified name 不會被 default client 的 active name 覆寫。</remarks>
        public static string? Encrypt(string? plainText, string? unifiedName) => GetDefaultClient().Encrypt(plainText, unifiedName);

        /// <inheritdoc cref="CryptoClient.Decrypt(string?)" />
        /// <remarks>輸入為空時回傳 <see langword="null"/>；非空資料會依尾綴的 unified name 載入金鑰。</remarks>
        public static string? Decrypt(string? encryptedDataWithUnifiedName) => GetDefaultClient().Decrypt(encryptedDataWithUnifiedName);

        /// <summary>
            /// 僅驗證外層格式，不代表資料已通過 authentication 或可使用目前金鑰解密。
        /// </summary>
        /// <param name="data">預期為 <c>Base64(payload).unifiedName</c> 的字串。</param>
        /// <returns>若外層含非空 payload、句點尾綴且 payload 可被 Base64 解碼則為 <see langword="true"/>；否則為 <see langword="false"/>。</returns>
        /// <exception cref="InvalidOperationException">尚未初始化 default client。</exception>
        /// <remarks>此方法不檢查 GCM tag、AAD、金鑰存在性或解密結果，因此不能當作驗證或授權檢查。</remarks>
        public static bool IsValidEncryptedFormat(string? data) => GetDefaultClient().IsValidEncryptedFormat(data);

        /// <inheritdoc cref="CryptoClient.GetUnifiedName(string?)" />
        /// <remarks>空輸入回傳 <see langword="null"/>；缺少有效尾綴時會拋出格式例外。</remarks>
        public static string? GetUnifiedName(string? encryptedDataWithUnifiedName) => GetDefaultClient().GetUnifiedName(encryptedDataWithUnifiedName);

        /// <inheritdoc cref="CryptoClient.GetUnifiedNameFromEncryptedData(string?)" />
        /// <remarks>此名稱是 <see cref="GetUnifiedName(string?)"/> 的相容別名。</remarks>
        public static string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName) => GetDefaultClient().GetUnifiedNameFromEncryptedData(encryptedDataWithUnifiedName);

        // Kept internal so tests can isolate process-global state without adding a
        // public reset operation that production callers could misuse.
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
