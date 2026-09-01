using System;
using System.Linq;
using System.Text;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.KeyGenerators;
using B2B.CryptoLib.Models;
using B2B.CryptoLib.Services;
using Xunit;

namespace B2B.CryptoLib.Tests
{
    public class CryptoServiceBehaviorTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(1024)]
        public void Aes_CbcCompatibility_RoundTripsSupportedPayloadLengths(int length)
        {
            var service = new CryptoService();
            var key = new SymmetricKeyModel
            {
                Key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(),
                IV = Enumerable.Range(32, 16).Select(value => (byte)value).ToArray()
            };
            var data = Enumerable.Range(0, length).Select(value => (byte)(value % 251)).ToArray();

            var encrypted = service.Encrypt(data, CryptoAlgorithmType.AES, key);

            Assert.NotEmpty(encrypted);
            Assert.Equal(data, service.Decrypt(encrypted, CryptoAlgorithmType.AES, key));
        }

        [Fact]
        public void Aes_CbcCompatibility_DecryptsFixedKnownAnswerVector()
        {
            var service = new CryptoService();
            var key = new SymmetricKeyModel
            {
                Key = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(),
                IV = Enumerable.Range(32, 16).Select(value => (byte)value).ToArray()
            };
            var encrypted = Convert.FromBase64String("xUpVPlcYQoFJfECbqrp8LVlERDN4uSLKjgeAVSLfDyo=");

            var plainText = service.Decrypt(encrypted, CryptoAlgorithmType.AES, key);

            Assert.Equal("legacy CBC fixture", Encoding.UTF8.GetString(plainText));
        }

        [Fact]
        public void EncryptAndDecrypt_RejectNullInput()
        {
            var service = new CryptoService();
            var key = new SymmetricKeyModel { Key = new byte[32], IV = new byte[16] };

            Assert.Throws<ArgumentNullException>(() => service.Encrypt<SymmetricKeyModel>(null, CryptoAlgorithmType.AES, key));
            Assert.Throws<ArgumentNullException>(() => service.Decrypt<SymmetricKeyModel>(null, CryptoAlgorithmType.AES, key));
        }

        [Fact]
        public void Encrypt_RejectsMismatchedAlgorithmAndKeyModel()
        {
            var service = new CryptoService();
            var aesKey = new SymmetricKeyModel { Key = new byte[32], IV = new byte[16] };

            Assert.Throws<NotSupportedException>(() => service.Encrypt(new byte[] { 1 }, CryptoAlgorithmType.RSA, aesKey));
            Assert.Throws<NotSupportedException>(() => service.Decrypt(new byte[] { 1 }, CryptoAlgorithmType.ECC, aesKey));
        }

        [Fact]
        public void RsaSignature_RejectsAlteredDataAndSignature()
        {
            ConfigureKeyGeneration();
            var service = new CryptoService();
            var rsa = new RsaKeyGenerator().GenerateKeyOnly();
            var data = Encoding.UTF8.GetBytes("RSA signature test");
            var signature = service.Sign(data, CryptoAlgorithmType.RSA, rsa);
            var alteredData = Encoding.UTF8.GetBytes("RSA signature test.");
            var alteredSignature = (byte[])signature.Clone();
            alteredSignature[0] ^= 0x01;

            Assert.True(service.Verify(data, signature, CryptoAlgorithmType.RSA, rsa));
            Assert.False(service.Verify(alteredData, signature, CryptoAlgorithmType.RSA, rsa));
            Assert.False(service.Verify(data, alteredSignature, CryptoAlgorithmType.RSA, rsa));
        }

        [Fact]
        public void EccSignature_RejectsAlteredData()
        {
            ConfigureKeyGeneration();
            var service = new CryptoService();
            var ecc = new EccKeyGenerator().GenerateKeyOnly();
            var data = Encoding.UTF8.GetBytes("ECDSA signature test");
            var signature = service.Sign(data, CryptoAlgorithmType.ECC, ecc);

            Assert.True(service.Verify(data, signature, CryptoAlgorithmType.ECC, ecc));
            Assert.False(service.Verify(Encoding.UTF8.GetBytes("ECDSA signature test."), signature, CryptoAlgorithmType.ECC, ecc));
        }

        private static void ConfigureKeyGeneration()
        {
            CryptoConfig.Override(new CryptoConfigModel
            {
                KeyDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "B2B.CryptoLib.Tests", Guid.NewGuid().ToString("N")),
                RSA = new RsaConfig { KeySize = 2048 },
                ECC = new EccConfig { Curve = EccCurveType.NistP256 }
            });
        }
    }
}
