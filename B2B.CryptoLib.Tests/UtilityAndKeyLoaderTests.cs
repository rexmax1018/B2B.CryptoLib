using System;
using System.IO;
using System.Text;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Factories;
using B2B.CryptoLib.Helpers;
using B2B.CryptoLib.KeyLoaders;
using B2B.CryptoLib.Models;
using Xunit;

namespace B2B.CryptoLib.Tests
{
    public class UtilityAndKeyLoaderTests
    {
        [Fact]
        public void Base64UrlSafe_RoundTripsBinaryData()
        {
            var source = new byte[] { 0xfb, 0xff, 0xff, 0x00, 0x01 };
            var encoded = Base64Utils.EncodeUrlSafe(source);

            Assert.DoesNotContain("+", encoded);
            Assert.DoesNotContain("/", encoded);
            Assert.DoesNotContain("=", encoded);
            Assert.Equal(source, Base64Utils.DecodeUrlSafe(encoded));
        }

        [Fact]
        public void Hex_RoundTripsAndRejectsInvalidInput()
        {
            var source = new byte[] { 0x00, 0x0f, 0xa0, 0xff };

            Assert.Equal("000FA0FF", HexUtils.Encode(source));
            Assert.Equal(source, HexUtils.Decode("000fa0ff"));
            Assert.Throws<FormatException>(() => HexUtils.Decode("ABC"));
            Assert.Throws<FormatException>(() => HexUtils.Decode("GG"));
        }

        [Fact]
        public void Padding_RoundTripsAndRejectsTamperedPadding()
        {
            var source = Encoding.UTF8.GetBytes("padding");
            var padded = PaddingUtils.ApplyPadding(source, 8);

            Assert.Equal(source, PaddingUtils.RemovePadding(padded));

            padded[padded.Length - 1] ^= 0x01;
            Assert.Throws<FormatException>(() => PaddingUtils.RemovePadding(padded));
        }

        [Fact]
        public void AesKeyLoader_LoadsBase64AndStreamRepresentations()
        {
            const string json = "{\"Key\":\"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=\",\"IV\":\"AAECAwQFBgcICQoLDA0ODw==\"}";
            var loader = new AesKeyLoader();
            var fromBase64 = loader.LoadFromBase64(Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var fromStream = loader.LoadFromStream(stream);

                Assert.Equal(32, fromBase64.Key.Length);
                Assert.Equal(16, fromBase64.IV.Length);
                Assert.Equal(fromBase64.Key, fromStream.Key);
                Assert.Equal(fromBase64.IV, fromStream.IV);
            }
        }

        [Fact]
        public void KeyLoaderFactory_ReturnsMatchingLoaderAndRejectsMismatch()
        {
            var factory = new KeyLoaderFactory();

            Assert.IsType<AesKeyLoader>(factory.Create<SymmetricKeyModel>(CryptoAlgorithmType.AES));
            Assert.Throws<NotSupportedException>(() => factory.Create<RsaKeyModel>(CryptoAlgorithmType.AES));
        }

        [Fact]
        public void CryptoConfig_LoadsAbsoluteFileAndGeneratesSafeKeyPaths()
        {
            var root = Path.Combine(Path.GetTempPath(), "B2B.CryptoLib.Tests", Guid.NewGuid().ToString("N"));
            var configPath = Path.Combine(root, "settings.json");
            Directory.CreateDirectory(root);
            File.WriteAllText(configPath, "{\"CryptoSuite\":{\"KeyDirectory\":\"keys\",\"RSA\":{\"KeySize\":2048},\"Unknown\":true}}", Encoding.UTF8);

            try
            {
                CryptoConfig.Load(configPath);

                Assert.Equal(2048, CryptoConfig.Current.RSA.KeySize);
                Assert.Equal(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keys", "RSA", "key.json"), CryptoConfig.GetKeyPath("RSA", "key.json"));
                Assert.EndsWith(".test", CryptoConfig.GenerateKeyFileName(".test"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
