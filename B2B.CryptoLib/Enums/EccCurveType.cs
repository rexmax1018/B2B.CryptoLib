namespace B2B.CryptoLib.Enums
{
    /// <summary>
    /// ECC 支援的橢圓曲線類型。
    /// </summary>
    /// <remarks>曲線選擇會寫入 <see cref="Models.EccKeyModel.Curve"/>，並影響離線產生器的領域參數。</remarks>
    public enum EccCurveType
    {
        /// <summary>NIST P-256，常用且相容性廣的 256-bit 曲線。</summary>
        NistP256,
        /// <summary>NIST P-384，提供較高安全強度的曲線。</summary>
        NistP384,
        /// <summary>NIST P-521，提供更高安全強度的曲線。</summary>
        NistP521,
        /// <summary>secp256k1，常見於特定區塊鏈與相容性需求。</summary>
        Secp256k1
    }
}
