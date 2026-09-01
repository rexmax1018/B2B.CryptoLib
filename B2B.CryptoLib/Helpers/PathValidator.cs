using System;
using System.IO;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 驗證檔案路徑是否位於應用程式指定的 Keys 安全根目錄內。
    /// </summary>
    /// <remarks>
    /// 這是舊版的固定根目錄輔助方法；新的金鑰組執行階段使用
    /// <see cref="PathSecurityHelper"/> 搭配每個用戶端的基底路徑。路徑驗證
    /// 只限制檔案位置，不代表檔案內容或私鑰權限已安全。
    /// </remarks>
    public static class PathValidator
    {
        private static readonly string SafeBaseDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys"));

        /// <summary>將輸入路徑解析為位於固定 Keys 根目錄內的完整路徑。</summary>
        /// <param name="unsafePath">待驗證的相對或絕對路徑。</param>
        /// <returns>通過根目錄前綴檢查的完整路徑。</returns>
        /// <exception cref="ArgumentException">路徑為 null、空或只含空白，或路徑格式無效。</exception>
        /// <exception cref="UnauthorizedAccessException">解析後路徑不在應用程式 Keys 根目錄下。</exception>
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
