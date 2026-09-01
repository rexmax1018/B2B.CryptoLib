using System;
using System.IO;
using B2B.CryptoLib.Helpers;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// Runtime 設定；不依賴宿主程式的 configuration provider。
    /// </summary>
    public sealed class CryptoOptions
    {
        /// <summary>
        /// Current、History 與 Update 金鑰組的根目錄。
        /// </summary>
        public required string KeyManagerBasePath { get; init; }

        /// <summary>
        /// 未指定 unifiedName 的加密操作所使用的金鑰名稱。留空時，呼叫端必須明確傳入名稱。
        /// </summary>
        public string? ActiveUnifiedName { get; init; }

        internal CryptoOptions Normalize()
        {
            if (string.IsNullOrWhiteSpace(KeyManagerBasePath))
                throw new ArgumentException("KeyManagerBasePath must be provided and cannot be empty.", nameof(KeyManagerBasePath));

            string normalizedPath;

            try
            {
                normalizedPath = Path.GetFullPath(KeyManagerBasePath);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                throw new ArgumentException("KeyManagerBasePath is not a valid path.", nameof(KeyManagerBasePath), ex);
            }

            var root = Path.GetPathRoot(normalizedPath);

            if (!string.Equals(normalizedPath, root, PathSecurityHelper.PathComparison))
                normalizedPath = normalizedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (ActiveUnifiedName is not null && !PathSecurityHelper.IsSafeUnifiedName(ActiveUnifiedName))
                throw new ArgumentException("ActiveUnifiedName may contain only letters, numbers, '_' and '-'.", nameof(ActiveUnifiedName));

            return new CryptoOptions
            {
                KeyManagerBasePath = normalizedPath,
                ActiveUnifiedName = ActiveUnifiedName
            };
        }

        internal static bool AreEquivalent(CryptoOptions left, CryptoOptions right)
        {
            return string.Equals(left.KeyManagerBasePath, right.KeyManagerBasePath, PathSecurityHelper.PathComparison)
                && string.Equals(left.ActiveUnifiedName, right.ActiveUnifiedName, StringComparison.Ordinal);
        }
    }
}
