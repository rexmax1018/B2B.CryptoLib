namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 以統一金鑰名稱封裝資料加解密的服務介面。
    /// </summary>
    public interface IDataEncryptionService
    {
        string Encrypt(string plainText, string unifiedName);

        string Decrypt(string encryptedDataWithUnifiedName);

        string GetUnifiedNameFromEncryptedData(string encryptedDataWithUnifiedName);

        bool IsValidEncryptedFormat(string data);
    }
}
