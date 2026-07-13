using System;

namespace B2B.CryptoLib.Helpers
{
    /// <summary>
    /// 提供 PKCS#7 Padding 與 Unpadding 工具方法。
    /// </summary>
    public static class PaddingUtils
    {
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
