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
    /// 舊版 .der 內容以此格式包裝 AES 金鑰與 IV，並非目前一般 RSA API 使用的 OAEP 格式。
    /// </summary>
    /// <remarks>
    /// 這個相容性入口刻意與 <see cref="CryptoService"/> 的 RSA-OAEP 路徑分離。
    /// 改用 OAEP、改變 PEM 解析或改變材料分隔符號都會使既有舊版
    /// <c>.der</c> 金鑰組無法解密，因此只能在明確的舊版分支使用。
    /// </remarks>
    public static class LegacyKeySetCrypto
    {
        /// <summary>以 RSA 公開金鑰和 PKCS#1 v1.5 加密舊版金鑰組材料。</summary>
        /// <param name="data">要包裝的 AES 金鑰／IV 材料位元組。</param>
        /// <param name="key">含 PEM 公開金鑰的 RSA 模型。</param>
        /// <returns>PKCS#1 v1.5 包裝後的位元組。</returns>
        /// <exception cref="ArgumentNullException">data 或 key 為 null。</exception>
        /// <exception cref="InvalidDataException">PEM 公開金鑰無法解析。</exception>
        public static byte[] Encrypt(byte[] data, RsaKeyModel key)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return Transform(data, key.PublicKey, true);
        }

        /// <summary>以 RSA 私密金鑰和 PKCS#1 v1.5 解密舊版金鑰組材料。</summary>
        /// <param name="encrypted">要解包的舊版位元組。</param>
        /// <param name="key">含 PEM 私密金鑰的 RSA 模型。</param>
        /// <returns>解包後的 AES 金鑰／IV 材料位元組。</returns>
        /// <exception cref="ArgumentNullException">encrypted 或 key 為 null。</exception>
        /// <exception cref="InvalidDataException">PEM 私密金鑰無法解析。</exception>
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
            // .der 材料是舊版 RSA PKCS#1 v1.5 契約；不要以目前公開 API
            // 使用的 OAEP 路徑取代它。
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
