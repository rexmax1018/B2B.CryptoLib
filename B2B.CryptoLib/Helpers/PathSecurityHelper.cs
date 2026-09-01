using System;
using System.IO;
using System.Text.RegularExpressions;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供金鑰目錄、檔案路徑與統一名稱的安全性驗證。
    /// </summary>
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
            if (string.IsNullOrWhiteSpace(unifiedName) || !SafeUnifiedNameRegex.IsMatch(unifiedName))
                throw new ArgumentException("Unified name contains invalid characters.", nameof(unifiedName));

            return unifiedName;
        }

        public static bool IsSafeUnifiedName(string unifiedName) => !string.IsNullOrWhiteSpace(unifiedName) && SafeUnifiedNameRegex.IsMatch(unifiedName);

        public static bool IsPathUnderDirectory(string targetPath, string baseDirectory) => !string.IsNullOrWhiteSpace(targetPath) && !string.IsNullOrWhiteSpace(baseDirectory) && Path.GetFullPath(targetPath).StartsWith(NormalizeDirectoryPath(baseDirectory), PathComparison);

        private static string NormalizeDirectoryPath(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }
}
