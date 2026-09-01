using System;
using System.Threading;
using System.Threading.Tasks;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib
{
    /// <summary>
    /// Process 內的 default <see cref="CryptoClient"/> facade。
    /// </summary>
    public static class Crypto
    {
        private static readonly object SyncRoot = new object();
        private static CryptoClient? _defaultClient;
        private static CryptoOptions? _defaultOptions;

        /// <summary>
        /// 設定 process 內的 default client。相同設定可重複呼叫，不同設定會明確失敗。
        /// </summary>
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
                Volatile.Write(ref _defaultOptions, normalized);
                Volatile.Write(ref _defaultClient, client);
            }
        }

        /// <summary>
        /// 明確執行一次 default client 的 Update 金鑰組發布。
        /// </summary>
        public static Task UpdateKeySetsAsync() => GetDefaultClient().UpdateKeySetsAsync();

        /// <inheritdoc cref="CryptoClient.Encrypt(string?)" />
        public static string? Encrypt(string? plainText) => GetDefaultClient().Encrypt(plainText);

        /// <inheritdoc cref="CryptoClient.Encrypt(string?, string?)" />
        public static string? Encrypt(string? plainText, string? unifiedName) => GetDefaultClient().Encrypt(plainText, unifiedName);

        /// <inheritdoc cref="CryptoClient.Decrypt(string?)" />
        public static string? Decrypt(string? encryptedDataWithUnifiedName) => GetDefaultClient().Decrypt(encryptedDataWithUnifiedName);

        /// <summary>
        /// 僅驗證外層格式，不代表資料已通過 authentication 或可使用目前金鑰解密。
        /// </summary>
        public static bool IsValidEncryptedFormat(string? data) => GetDefaultClient().IsValidEncryptedFormat(data);

        /// <inheritdoc cref="CryptoClient.GetUnifiedName(string?)" />
        public static string? GetUnifiedName(string? encryptedDataWithUnifiedName) => GetDefaultClient().GetUnifiedName(encryptedDataWithUnifiedName);

        /// <inheritdoc cref="CryptoClient.GetUnifiedNameFromEncryptedData(string?)" />
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
