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
    public static class LegacyKeySetCrypto
    {
        public static byte[] Encrypt(byte[] data, RsaKeyModel key)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return Transform(data, key.PublicKey, true);
        }

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
