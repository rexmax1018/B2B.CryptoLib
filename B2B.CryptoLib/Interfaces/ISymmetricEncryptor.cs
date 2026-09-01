namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 對稱式加密介面。
    /// </summary>
    /// <remarks>設定的 key 與 IV 屬於實作 instance 狀態；若要並行處理，請依實作的 thread-safety 契約隔離 instance。</remarks>
    public interface ISymmetricEncryptor : IEncryptor
    {
        /// <summary>設定對稱式加密的 key 與 initialization vector。</summary>
        /// <param name="key">演算法要求長度的 key bytes。</param>
        /// <param name="iv">演算法要求長度的 initialization vector bytes。</param>
        /// <exception cref="System.ArgumentNullException">key 或 IV 為 null。</exception>
        /// <exception cref="System.ArgumentException">key 或 IV 長度不符合實作要求。</exception>
        void SetKey(byte[] key, byte[] iv);
    }
}
