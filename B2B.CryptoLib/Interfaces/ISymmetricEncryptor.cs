namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 對稱式加密介面。
    /// </summary>
    /// <remarks>設定的金鑰與 IV 屬於實作執行個體狀態；若要並行處理，請依實作的執行緒安全契約隔離執行個體。</remarks>
    public interface ISymmetricEncryptor : IEncryptor
    {
        /// <summary>設定對稱式加密的金鑰與初始化向量。</summary>
        /// <param name="key">符合演算法要求長度的金鑰位元組。</param>
        /// <param name="iv">符合演算法要求長度的初始化向量位元組。</param>
        /// <exception cref="System.ArgumentNullException">金鑰或 IV 為 null。</exception>
        /// <exception cref="System.ArgumentException">金鑰或 IV 長度不符合實作要求。</exception>
        void SetKey(byte[] key, byte[] iv);
    }
}
