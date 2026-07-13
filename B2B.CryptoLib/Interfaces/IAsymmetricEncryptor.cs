namespace B2B.CryptoLib.Interfaces
{
    /// <summary>
    /// 非對稱式加密介面。
    /// </summary>
    public interface IAsymmetricEncryptor : IEncryptor
    {
        void SetPublicKey(string publicKey);

        void SetPrivateKey(string privateKey);
    }
}
