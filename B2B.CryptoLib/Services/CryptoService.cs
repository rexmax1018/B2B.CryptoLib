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
    public class CryptoService : ICryptoService
    {
        /// <summary>
        /// 使用指定演算法與金鑰模型加密位元組資料。
        /// </summary>
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
        public byte[] Sign<TKeyModel>(byte[] data, CryptoAlgorithmType algorithm, TKeyModel privateKeyModel) where TKeyModel : class => SignOrVerify(data, null, algorithm, privateKeyModel, true)!;

        /// <summary>
        /// 使用公鑰驗證數位簽章。
        /// </summary>
        public bool Verify<TKeyModel>(byte[] data, byte[] signature, CryptoAlgorithmType algorithm, TKeyModel publicKeyModel) where TKeyModel : class => SignOrVerify(data, signature, algorithm, publicKeyModel, false) != null;

        private static byte[] TransformAes(byte[] data, SymmetricKeyModel key, bool encrypt)
        {
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
