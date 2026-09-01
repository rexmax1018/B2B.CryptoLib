using System;
using System.IO;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace B2B.CryptoLib.Services
{
    /// <summary>
    /// 實作 AES、RSA 與 ECC 的加密、解密、簽章及驗章服務。
    /// </summary>
    /// <remarks>
    /// AES 的低階路徑維持 AES-CBC + PKCS#7，以讀取既有 legacy payload；RSA 資料
    /// 加密維持 OAEP；RSA 與 ECC 簽章分別使用 SHA-256 with RSA 與 SHA-256 with
    /// ECDSA。這些 padding、signature algorithm 與 PEM 解析規則是相容性邊界，
    /// 不應因更換 Bouncy Castle package identity 而改寫。
    /// </remarks>
    public class CryptoService : ICryptoService
    {
        /// <summary>
        /// 使用指定演算法與金鑰模型加密位元組資料。
        /// </summary>
        /// <typeparam name="TKeyModel">實際 key model 的型別。</typeparam>
        /// <param name="data">要加密的 bytes；不可為 <see langword="null"/>。</param>
        /// <param name="algorithm">AES 或 RSA；ECC 僅支援簽章，不支援此方法的資料加密。</param>
        /// <param name="keyModel">AES 的 <see cref="SymmetricKeyModel"/> 或 RSA 的 <see cref="RsaKeyModel"/>。</param>
        /// <returns>加密後的 bytes。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> 為 <see langword="null"/>。</exception>
        /// <exception cref="NotSupportedException">演算法與 key model 不相容。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">金鑰、padding 或資料長度不符合底層 primitive 要求。</exception>
        public byte[] Encrypt<TKeyModel>(byte[]? data, CryptoAlgorithmType algorithm, TKeyModel keyModel) where TKeyModel : class
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (algorithm == CryptoAlgorithmType.AES && keyModel is SymmetricKeyModel aes)
                return TransformAes(data, aes, true);

            if (algorithm == CryptoAlgorithmType.RSA && keyModel is RsaKeyModel rsa)
                return TransformRsa(data, rsa.PublicKey, true);

            throw new NotSupportedException($"不支援的加密演算法或金鑰模型：{algorithm}");
        }

        /// <summary>
        /// 使用指定演算法與金鑰模型解密位元組資料。
        /// </summary>
        /// <typeparam name="TKeyModel">實際 key model 的型別。</typeparam>
        /// <param name="encrypted">要解密的 bytes；不可為 <see langword="null"/>。</param>
        /// <param name="algorithm">AES 或 RSA；ECC 僅支援簽章，不支援此方法的資料解密。</param>
        /// <param name="keyModel">AES 的 <see cref="SymmetricKeyModel"/> 或 RSA 的 <see cref="RsaKeyModel"/>。</param>
        /// <returns>解密後的 bytes。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="encrypted"/> 為 <see langword="null"/>。</exception>
        /// <exception cref="NotSupportedException">演算法與 key model 不相容。</exception>
        /// <exception cref="System.Security.Cryptography.CryptographicException">密文、padding 或金鑰無法解密。</exception>
        public byte[] Decrypt<TKeyModel>(byte[]? encrypted, CryptoAlgorithmType algorithm, TKeyModel keyModel) where TKeyModel : class
        {
            if (encrypted == null)
                throw new ArgumentNullException(nameof(encrypted));

            if (algorithm == CryptoAlgorithmType.AES && keyModel is SymmetricKeyModel aes)
                return TransformAes(encrypted, aes, false);

            if (algorithm == CryptoAlgorithmType.RSA && keyModel is RsaKeyModel rsa)
                return TransformRsa(encrypted, rsa.PrivateKey, false);

            throw new NotSupportedException($"不支援的解密演算法或金鑰模型：{algorithm}");
        }

        /// <summary>
        /// 使用私鑰對資料產生數位簽章。
        /// </summary>
        /// <typeparam name="TKeyModel">RSA 或 ECC key model 的型別。</typeparam>
        /// <param name="data">要簽章的 bytes。</param>
        /// <param name="algorithm">RSA 或 ECC；選擇實際的 signature algorithm。</param>
        /// <param name="privateKeyModel">含 PEM 私鑰的 <see cref="RsaKeyModel"/> 或 <see cref="EccKeyModel"/>。</param>
        /// <returns>簽章 bytes。</returns>
        /// <exception cref="ArgumentNullException">必要資料或 key model 為 <see langword="null"/>。</exception>
        /// <exception cref="NotSupportedException">model 不是可簽章的 RSA/ECC model。</exception>
        /// <exception cref="InvalidDataException">PEM 私鑰無法解析。</exception>
        public byte[] Sign<TKeyModel>(byte[] data, CryptoAlgorithmType algorithm, TKeyModel privateKeyModel) where TKeyModel : class => SignOrVerify(data, null, algorithm, privateKeyModel, true)!;

        /// <summary>
        /// 使用公鑰驗證數位簽章。
        /// </summary>
        /// <typeparam name="TKeyModel">RSA 或 ECC key model 的型別。</typeparam>
        /// <param name="data">原始簽章資料。</param>
        /// <param name="signature">要驗證的簽章 bytes。</param>
        /// <param name="algorithm">RSA 或 ECC；必須與產生簽章時的演算法相同。</param>
        /// <param name="publicKeyModel">含 PEM 公鑰的 <see cref="RsaKeyModel"/> 或 <see cref="EccKeyModel"/>。</param>
        /// <returns>簽章有效時為 <see langword="true"/>，無效時為 <see langword="false"/>。</returns>
        /// <exception cref="ArgumentNullException">必要資料、簽章或 key model 為 <see langword="null"/>。</exception>
        /// <exception cref="NotSupportedException">model 不是可驗章的 RSA/ECC model。</exception>
        /// <exception cref="InvalidDataException">PEM 公鑰無法解析。</exception>
        public bool Verify<TKeyModel>(byte[] data, byte[] signature, CryptoAlgorithmType algorithm, TKeyModel publicKeyModel) where TKeyModel : class => SignOrVerify(data, signature, algorithm, publicKeyModel, false) != null;

        private static byte[] TransformAes(byte[] data, SymmetricKeyModel key, bool encrypt)
        {
            // Keep AES-CBC plus PKCS#7 because the no-marker branch in
            // DataEncryptionService must continue to decrypt legacy payloads.
            var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()));

            cipher.Init(encrypt, new ParametersWithIV(new KeyParameter(key.Key), key.IV));

            var output = new byte[cipher.GetOutputSize(data.Length)];
            var length = cipher.ProcessBytes(data, 0, data.Length, output, 0);

            length += cipher.DoFinal(output, length);

            if (length == output.Length)
                return output;

            var result = new byte[length];

            Buffer.BlockCopy(output, 0, result, 0, length);

            return result;
        }

        private static byte[] TransformRsa(byte[] data, string pem, bool encrypt)
        {
            // OAEP is the current public RSA contract. Legacy key-set material uses
            // PKCS#1 v1.5 in LegacyKeySetCrypto and must remain non-interchangeable.
            var cipher = new OaepEncoding(new RsaEngine());

            cipher.Init(encrypt, ReadKey(pem));

            return cipher.ProcessBlock(data, 0, data.Length);
        }

        private static byte[]? SignOrVerify<T>(byte[] data, byte[]? signature, CryptoAlgorithmType algorithm, T model, bool sign) where T : class
        {
            string pem;
            string algorithmName;

            if (model is RsaKeyModel rsa)
            {
                pem = sign ? rsa.PrivateKey : rsa.PublicKey;
                algorithmName = "SHA-256withRSA";
            }
            else if (model is EccKeyModel ecc)
            {
                pem = sign ? ecc.PrivateKey : ecc.PublicKey;
                algorithmName = "SHA-256withECDSA";
            }
            else
                throw new NotSupportedException($"不支援的簽章金鑰模型：{algorithm}");

            var signer = SignerUtilities.GetSigner(algorithmName);

            signer.Init(sign, ReadKey(pem));
            signer.BlockUpdate(data, 0, data.Length);

            if (sign)
                return signer.GenerateSignature();

            return signer.VerifySignature(signature) ? new byte[0] : null;
        }

        private static AsymmetricKeyParameter ReadKey(string pem)
        {
            // Preserve both PEM key pairs and standalone public keys: the .pub/.priv
            // layout is a deployment contract, not an incidental parser detail.
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
