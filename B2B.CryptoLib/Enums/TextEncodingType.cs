namespace B2B.CryptoLib.Enums
{
    /// <summary>
    /// 支援的文字編碼格式。
    /// </summary>
    /// <remarks>這些值保留供舊版設定與資料模型使用；高階 GCM v2 封裝的文字與 AAD 固定使用 UTF-8。</remarks>
    public enum TextEncodingType
    {
        /// <summary>UTF-8 可變長度編碼。</summary>
        UTF8,
        /// <summary>UTF-16 編碼。</summary>
        UTF16,
        /// <summary>UTF-32 編碼。</summary>
        UTF32,
        /// <summary>ASCII 編碼。</summary>
        ASCII
    }
}
