using System;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供 PKCS#7 Padding 與 Unpadding 工具方法。
    /// </summary>
    /// <remarks>
    /// 這些 helper 只處理 padding bytes；它們不驗證 ciphertext authentication。
    /// legacy AES-CBC 解密仍依賴 PKCS#7，請不要以移除 helper 來替代相容路徑。
    /// </remarks>
    public static class PaddingUtils
    {
        /// <summary>依指定 block size 套用 PKCS#7 padding。</summary>
        /// <param name="data">要補齊的資料；空陣列可接受並會產生完整一個 block。</param>
        /// <param name="blockSize">block size，必須介於 1 與 255 bytes。</param>
        /// <returns>包含 padding 的新陣列；不修改輸入陣列。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> 為 null。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="blockSize"/> 不在合法範圍。</exception>
        public static byte[] ApplyPadding(byte[] data, int blockSize)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (blockSize <= 0 || blockSize > 255)
                throw new ArgumentOutOfRangeException(nameof(blockSize));

            var paddingLength = blockSize - data.Length % blockSize;
            var padded = new byte[data.Length + paddingLength];

            Buffer.BlockCopy(data, 0, padded, 0, data.Length);

            for (var i = data.Length; i < padded.Length; i++)
                padded[i] = (byte)paddingLength;

            return padded;
        }

        /// <summary>移除並驗證資料尾端的 PKCS#7 padding。</summary>
        /// <param name="paddedData">至少包含一個 padding byte 的資料。</param>
        /// <returns>移除 padding 後的新陣列；不修改輸入陣列。</returns>
        /// <exception cref="ArgumentException">資料為 null 或空陣列。</exception>
        /// <exception cref="FormatException">padding 長度為零、超出資料長度或 bytes 不一致。</exception>
        public static byte[] RemovePadding(byte[] paddedData)
        {
            if (paddedData == null || paddedData.Length == 0)
                throw new ArgumentException("資料不可為空", nameof(paddedData));

            var paddingLength = paddedData[paddedData.Length - 1];

            if (paddingLength == 0 || paddingLength > paddedData.Length)
                throw new FormatException("Padding 資料不合法");

            for (var i = paddedData.Length - paddingLength; i < paddedData.Length; i++)
                if (paddedData[i] != paddingLength)
                    throw new FormatException("Padding 格式錯誤");

            var original = new byte[paddedData.Length - paddingLength];

            Buffer.BlockCopy(paddedData, 0, original, 0, original.Length);

            return original;
        }
    }
}
