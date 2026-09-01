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
    /// 這個輔助方法先正規化再做目錄邊界檢查，並限制統一名稱
    /// 為固定允許清單，避免路徑穿越、絕對路徑逃逸與把密文輸入轉成任意檔名。
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

            // 比較前先正規化，避免 ".." 與替代分隔符號繞過保護秘密材料
            // 的金鑰根目錄邊界。
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

            // 完整的既有檔案檢查可避免呼叫端把受信任的金鑰根目錄
            // 變成任意讀取原語。
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
            // 此允許清單刻意比一般檔名更嚴格：統一名稱同時會成為
            // AAD 值與金鑰組檔名。
            if (string.IsNullOrWhiteSpace(unifiedName) || !SafeUnifiedNameRegex.IsMatch(unifiedName))
                throw new ArgumentException("Unified name contains invalid characters.", nameof(unifiedName));

            return unifiedName;
        }

        public static bool IsSafeUnifiedName(string unifiedName) => !string.IsNullOrWhiteSpace(unifiedName) && SafeUnifiedNameRegex.IsMatch(unifiedName);

        // NormalizeDirectoryPath 以分隔符號結尾，因此像 "KeysBackup" 的同層目錄
        // 不會誤符合受信任的 "Keys" 目錄前綴。
        public static bool IsPathUnderDirectory(string targetPath, string baseDirectory) => !string.IsNullOrWhiteSpace(targetPath) && !string.IsNullOrWhiteSpace(baseDirectory) && Path.GetFullPath(targetPath).StartsWith(NormalizeDirectoryPath(baseDirectory), PathComparison);

        private static string NormalizeDirectoryPath(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
