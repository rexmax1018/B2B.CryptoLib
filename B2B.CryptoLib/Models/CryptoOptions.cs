using System;
using System.IO;
using B2B.CryptoLib.Helpers;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// Runtime 設定；不依賴宿主程式的 configuration provider。
    /// </summary>
    /// <remarks>
    /// 設定在 <see cref="CryptoClient"/> 建構時會被正規化並複製。此模型的 init
    /// 屬性適合在建立後視為 immutable 使用；它不會自行載入、產生或發布任何金鑰。
    /// <para>
    /// <see cref="KeyManagerBasePath"/> 必須是非空路徑。<see cref="ActiveUnifiedName"/>
    /// 可省略，但省略後必須在每次加密時使用明確 unified name overload。
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new CryptoOptions
    /// {
    ///     KeyManagerBasePath = @"C:\CryptoKeys",
    ///     ActiveUnifiedName = "tenant-a"
    /// };
    /// </code>
    /// </example>
    public sealed class CryptoOptions
    {
        /// <summary>
        /// Current、History 與 Update 金鑰組的根目錄。
        /// </summary>
        /// <value>可為相對或絕對路徑；client 會轉為完整路徑並建立必要的三個子目錄。</value>
        public required string KeyManagerBasePath { get; init; }

        /// <summary>
        /// 未指定 unifiedName 的加密操作所使用的金鑰名稱。留空時，呼叫端必須明確傳入名稱。
        /// </summary>
        /// <value>只能包含英數字元、底線與連字號；<see langword="null"/> 表示不設定預設名稱。</value>
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
