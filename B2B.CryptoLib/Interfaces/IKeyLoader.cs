using System.IO;
namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 從檔案、字串、Base64 或資料流載入金鑰的介面。
    /// </summary>
    /// <remarks>
    /// 載入器會把序列化內容轉成記憶體中的模型；它不負責路徑安全、金鑰輪替、
    /// 權限或秘密清除。<see cref="LoadFromStream(Stream)"/> 會消費目前資料流
    /// 的文字內容，呼叫端應依實作契約管理資料流的生命週期。
    /// </remarks>
    public interface IKeyLoader<T> where T : class
    {
        /// <summary>從檔案讀取並反序列化金鑰模型。</summary>
        /// <param name="path">要讀取的檔案路徑。</param>
        /// <returns>反序列化後的 <typeparamref name="T"/>。</returns>
        /// <exception cref="System.ArgumentException">路徑為空或無效。</exception>
        /// <exception cref="System.IO.FileNotFoundException">檔案不存在。</exception>
        /// <exception cref="System.IO.InvalidDataException">檔案不是有效的模型內容。</exception>
        T LoadFromFile(string path);

        /// <summary>從序列化文字讀取金鑰模型。</summary>
        /// <param name="content">JSON 或實作所支援的文字內容。</param>
        /// <returns>反序列化後的 <typeparamref name="T"/>。</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="content"/> 為 null。</exception>
        /// <exception cref="System.IO.InvalidDataException">內容無法解析。</exception>
        T LoadFromString(string content);

        /// <summary>從 Base64 編碼的序列化文字讀取金鑰模型。</summary>
        /// <param name="base64">包含 UTF-8 序列化內容的標準 Base64 字串。</param>
        /// <returns>反序列化後的 <typeparamref name="T"/>。</returns>
        /// <exception cref="System.ArgumentException">Base64 為空或無效。</exception>
        /// <exception cref="System.IO.InvalidDataException">解碼後內容無法解析。</exception>
        T LoadFromBase64(string base64);

        /// <summary>從目前位置讀取資料流並反序列化金鑰模型。</summary>
        /// <param name="stream">包含 UTF-8 序列化內容且可讀的資料流。</param>
        /// <returns>反序列化後的 <typeparamref name="T"/>。</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream"/> 為 null。</exception>
        /// <exception cref="System.IO.InvalidDataException">資料流內容無法解析。</exception>
        T LoadFromStream(Stream stream);
    }
}
