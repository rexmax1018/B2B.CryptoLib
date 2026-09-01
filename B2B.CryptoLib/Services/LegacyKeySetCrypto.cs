using System;
using System.IO;
using B2B.CryptoLib.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;

namespace B2B.CryptoLib.Services
{
    /// <summary>
    /// 處理舊版 KeyStore 金鑰組的 RSA PKCS#1 v1.5 包裝格式。
    /// 舊版 .der 內容以此格式包裝 AES Key 與 IV，並非目前一般 RSA API 使用的 OAEP 格式。
    /// </summary>
    /// <remarks>
    /// 這個相容性入口刻意與 <see cref="CryptoService"/> 的 RSA-OAEP path 分離。
    /// 改用 OAEP、改變 PEM 解析或改變 material separator 都會使既有 legacy
    /// <c>.der</c> key set 無法解密，因此只能在明確的 legacy branch 使用。
    /// </remarks>
    public static class LegacyKeySetCrypto
    {
        /// <summary>以 RSA public key 和 PKCS#1 v1.5 加密 legacy key-set material。</summary>
        /// <param name="data">要包裝的 AES key/IV material bytes。</param>
        /// <param name="key">含 PEM public key 的 RSA model。</param>
        /// <returns>PKCS#1 v1.5 wrapped bytes。</returns>
        /// <exception cref="ArgumentNullException">data 或 key 為 null。</exception>
        /// <exception cref="InvalidDataException">PEM public key 無法解析。</exception>
        public static byte[] Encrypt(byte[] data, RsaKeyModel key)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return Transform(data, key.PublicKey, true);
        }

        /// <summary>以 RSA private key 和 PKCS#1 v1.5 解密 legacy key-set material。</summary>
        /// <param name="encrypted">要解包的 legacy bytes。</param>
        /// <param name="key">含 PEM private key 的 RSA model。</param>
        /// <returns>解包後的 AES key/IV material bytes。</returns>
        /// <exception cref="ArgumentNullException">encrypted 或 key 為 null。</exception>
        /// <exception cref="InvalidDataException">PEM private key 無法解析。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">PKCS#1 v1.5 解包失敗。</exception>
        public static byte[] Decrypt(byte[] encrypted, RsaKeyModel key)
        {
            if (encrypted == null)
                throw new ArgumentNullException(nameof(encrypted));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return Transform(encrypted, key.PrivateKey, false);
        }

        private static byte[] Transform(byte[] data, string pem, bool encrypt)
        {
            // .der material is a legacy RSA PKCS#1 v1.5 contract; do not replace
            // it with the OAEP path used by the current public API.
            var cipher = new Pkcs1Encoding(new RsaEngine());
            cipher.Init(encrypt, ReadKey(pem));

            return cipher.ProcessBlock(data, 0, data.Length);
        }

        private static AsymmetricKeyParameter ReadKey(string pem)
        {
            using (var reader = new StringReader(pem))
            {
                var value = new PemReader(reader).ReadObject();

                if (value is AsymmetricCipherKeyPair pair)
                    return pair.Private;

                if (value is AsymmetricKeyParameter key)
                    return key;

                throw new InvalidDataException("無法讀取 PEM 金鑰");
            }
        }
    }
}
