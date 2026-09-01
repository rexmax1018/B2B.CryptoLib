namespace B2B.CryptoLib.Enums
{
    /// <summary>
    /// 支援的金鑰演算法類型。
    /// </summary>
    /// <remarks>
    /// AES 用於對稱式資料處理，RSA 用於目前的 OAEP 金鑰包裝與 RSA 簽章，
    /// ECC 用於 ECDSA 簽章驗章；演算法與金鑰模型必須配對。
    /// </remarks>
    public enum CryptoAlgorithmType
    {
        /// <summary>Advanced Encryption Standard；支援 AES 金鑰／IV 模型。</summary>
        AES,
        /// <summary>RSA；資料加密使用 OAEP，舊版金鑰組材料使用獨立 PKCS#1 v1.5 路徑。</summary>
        RSA,
        /// <summary>Elliptic Curve Cryptography；目前用於 ECDSA 簽章與驗章。</summary>
        ECC
    }
}
