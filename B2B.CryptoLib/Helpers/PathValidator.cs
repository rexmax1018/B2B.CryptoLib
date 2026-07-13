using System;
using System.IO;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 驗證檔案路徑是否位於應用程式指定的 Keys 安全根目錄內。
    /// </summary>
    public static class PathValidator
    {
        private static readonly string SafeBaseDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys"));

        public static string GetSafePath(string unsafePath)
        {
            if (string.IsNullOrWhiteSpace(unsafePath))
                throw new ArgumentException("Path cannot be empty");

            var fullPath = Path.GetFullPath(unsafePath);

            if (!fullPath.StartsWith(SafeBaseDirectory, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Attempted to access a path outside of the safe directory.");

            return fullPath;
        }
    }
}
