using System;
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
                if (_defaultClient is not null)
                {
                    if (_defaultOptions is not null && CryptoOptions.AreEquivalent(_defaultOptions, normalized))
                        return;

                    throw new InvalidOperationException("Crypto has already been initialized with a different configuration. Create an isolated CryptoClient for another key context.");
                }

                var client = new CryptoClient(normalized);
                _defaultClient = client;
                _defaultOptions = normalized;
            }
        }

        /// <inheritdoc cref="CryptoClient.Encrypt(string?)" />
        public static string? Encrypt(string? plainText) => GetDefaultClient().Encrypt(plainText);

        /// <inheritdoc cref="CryptoClient.Encrypt(string?, string?)" />
        public static string? Encrypt(string? plainText, string? unifiedName) => GetDefaultClient().Encrypt(plainText, unifiedName);

        /// <inheritdoc cref="CryptoClient.Decrypt(string?)" />
        public static string? Decrypt(string? encryptedDataWithUnifiedName) => GetDefaultClient().Decrypt(encryptedDataWithUnifiedName);

        /// <inheritdoc cref="CryptoClient.IsEncrypted(string?)" />
        public static bool IsEncrypted(string? data) => GetDefaultClient().IsEncrypted(data);

        /// <inheritdoc cref="CryptoClient.IsValidEncryptedFormat(string?)" />
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
                _defaultClient = null;
                _defaultOptions = null;
            }
        }

        private static CryptoClient GetDefaultClient()
        {
            lock (SyncRoot)
            {
                return _defaultClient ?? throw new InvalidOperationException("Crypto has not been initialized. Call Crypto.Initialize(...) before using it.");
            }
        }
    }
}
