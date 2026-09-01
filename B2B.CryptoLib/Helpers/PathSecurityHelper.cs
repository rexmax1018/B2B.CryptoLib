using System;
using System.IO;
using System.Text.RegularExpressions;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供金鑰目錄、檔案路徑與統一名稱的安全性驗證。
    /// </summary>
    /// <remarks>
    /// 金鑰檔案路徑來自部署設定或密文尾綴，不能直接信任為檔案系統路徑。
    /// 這個 helper 先正規化再做 directory-boundary 檢查，並限制 unified name
    /// 為固定 allow-list，避免 traversal、絕對路徑逃逸與把密文輸入轉成任意檔名。
    /// </remarks>
    internal static class PathSecurityHelper
    {
        private static readonly Regex SafeUnifiedNameRegex = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        internal static StringComparison PathComparison => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        public static string ValidateAndGetSafeDirectoryPath(string baseDirectory, string targetDirectory, bool requireExists = true)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentException("Base directory cannot be empty.", nameof(baseDirectory));

            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new ArgumentException("Target directory cannot be empty.", nameof(targetDirectory));

            // Normalize before comparing so ".." and alternate separators cannot
            // bypass the key-root boundary that protects secret material.
            var fullBasePath = NormalizeDirectoryPath(baseDirectory);
            var fullTargetPath = NormalizeDirectoryPath(targetDirectory);

            if (!IsPathUnderDirectory(fullTargetPath, fullBasePath))
                throw new UnauthorizedAccessException("Target directory is outside of the allowed base directory.");

            if (requireExists && !Directory.Exists(fullTargetPath))
                throw new DirectoryNotFoundException($"Directory not found: {fullTargetPath}");

            return fullTargetPath;
        }

        public static string ValidateAndGetSafeExistingFilePath(string baseDirectory, string filePath)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentException("Base directory cannot be empty.", nameof(baseDirectory));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));

            // A complete existing-file check prevents callers from turning a
            // trusted key root into an arbitrary read primitive.
            var fullBasePath = NormalizeDirectoryPath(baseDirectory);
            var fullFilePath = Path.GetFullPath(filePath);

            if (!IsPathUnderDirectory(fullFilePath, fullBasePath))
                throw new UnauthorizedAccessException("File path is outside of the allowed base directory.");

            var fileName = Path.GetFileName(fullFilePath);

            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("File name contains invalid characters.", nameof(filePath));

            if (!File.Exists(fullFilePath))
                throw new FileNotFoundException("File not found.", fullFilePath);

            return fullFilePath;
        }

        public static string ValidateUnifiedName(string unifiedName)
        {
            // The allow-list is deliberately narrower than a general filename:
            // unified names become both an AAD value and a key-set filename.
            if (string.IsNullOrWhiteSpace(unifiedName) || !SafeUnifiedNameRegex.IsMatch(unifiedName))
                throw new ArgumentException("Unified name contains invalid characters.", nameof(unifiedName));

            return unifiedName;
        }

        public static bool IsSafeUnifiedName(string unifiedName) => !string.IsNullOrWhiteSpace(unifiedName) && SafeUnifiedNameRegex.IsMatch(unifiedName);

        // NormalizeDirectoryPath ends with a separator so a sibling such as
        // "KeysBackup" cannot match the trusted "Keys" directory prefix.
        public static bool IsPathUnderDirectory(string targetPath, string baseDirectory) => !string.IsNullOrWhiteSpace(targetPath) && !string.IsNullOrWhiteSpace(baseDirectory) && Path.GetFullPath(targetPath).StartsWith(NormalizeDirectoryPath(baseDirectory), PathComparison);

        private static string NormalizeDirectoryPath(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
