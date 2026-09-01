namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 非對稱式加密介面。
    /// </summary>
    /// <remarks>
    /// 實作由呼叫端設定 key 後執行 byte-array 加解密。CryptoLib 的高階
    /// <see cref="ICryptoService"/> RSA 路徑使用 OAEP；legacy key-set 的 PKCS#1 v1.5
    /// material 不應透過此介面假設為可互換格式。
    /// </remarks>
    public interface IAsymmetricEncryptor : IEncryptor
    {
        /// <summary>設定供加密使用的 PEM 公鑰。</summary>
        /// <param name="publicKey">可由實作解析的 PEM 公鑰文字。</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="publicKey"/> 為 null；實作也可能對格式錯誤拋出資料例外。</exception>
        void SetPublicKey(string publicKey);

        /// <summary>設定供解密使用的 PEM 私鑰。</summary>
        /// <param name="privateKey">可由實作解析的 PEM 私鑰文字。</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="privateKey"/> 為 null；實作也可能對格式錯誤拋出資料例外。</exception>
        void SetPrivateKey(string privateKey);
    }
}
