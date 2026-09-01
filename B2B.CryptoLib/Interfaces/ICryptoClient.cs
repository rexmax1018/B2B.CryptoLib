namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 不需要 DI 的高階文字加解密 client。
    /// </summary>
    public interface ICryptoClient
    {
        string? Encrypt(string? plainText);

        string? Encrypt(string? plainText, string? unifiedName);

        string? Decrypt(string? encryptedDataWithUnifiedName);

        bool IsEncrypted(string? data);

        bool IsValidEncryptedFormat(string? data);

        string? GetUnifiedName(string? encryptedDataWithUnifiedName);

        string? GetUnifiedNameFromEncryptedData(string? encryptedDataWithUnifiedName);
    }
}
