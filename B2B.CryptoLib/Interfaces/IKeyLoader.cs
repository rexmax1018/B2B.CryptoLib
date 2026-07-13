using System.IO;
namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 從檔案、字串、Base64 或資料流載入金鑰的介面。
    /// </summary>
    public interface IKeyLoader<T> where T : class
    {
        T LoadFromFile(string path); T LoadFromString(string content); T LoadFromBase64(string base64); T LoadFromStream(Stream stream);
    }
}
