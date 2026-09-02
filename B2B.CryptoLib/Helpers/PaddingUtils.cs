using System;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供 PKCS#7 填充與去除填充工具方法。
    /// </summary>
    /// <remarks>
    /// 這些輔助方法只處理填充位元組；它們不驗證密文的訊息驗證。
    /// 舊版 AES-CBC 解密仍依賴 PKCS#7，請不要以移除輔助方法來替代相容路徑。
    /// </remarks>
    public static class PaddingUtils
    {
        /// <summary>依指定區塊大小套用 PKCS#7 填充。</summary>
        /// <param name="data">要補齊的資料；空陣列可接受並會產生完整一個 block。</param>
        /// <param name="blockSize">區塊大小，必須介於 1 與 255 個位元組。</param>
        /// <returns>包含填充的新陣列；不修改輸入陣列。</returns>
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

        /// <summary>移除並驗證資料尾端的 PKCS#7 填充。</summary>
        /// <param name="paddedData">至少包含一個填充位元組的資料。</param>
        /// <returns>移除填充後的新陣列；不修改輸入陣列。</returns>
        /// <exception cref="ArgumentException">資料為 null 或空陣列。</exception>
        /// <exception cref="FormatException">填充長度為零、超出資料長度或位元組不一致。</exception>
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
