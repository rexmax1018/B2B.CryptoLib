using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.KeyGenerators;
using B2B.CryptoLib.Models;
using B2B.CryptoLib.Services;
using B2BCryptoConfig = B2B.CryptoLib.Config.CryptoConfig;
using Xunit;

namespace B2B.CryptoLib.Tests
{
    public sealed class CryptoClientTests : IDisposable
    {
        private readonly string _testRoot = Path.Combine(Path.GetTempPath(), "B2B.CryptoLib.Tests", Guid.NewGuid().ToString("N"));

        public CryptoClientTests()
        {
            Crypto.ResetForTests();
        }

        public void Dispose()
        {
            Crypto.ResetForTests();

            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [Fact]
        public void CryptoClient_RoundTripsUsingConfiguredActiveUnifiedName()
        {
            const string unifiedName = "active-key";
            var keyRoot = Path.Combine(_testRoot, "key-sets");
            WriteV2KeySet(keyRoot, unifiedName);

            var client = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = unifiedName
            });

            var encrypted = client.Encrypt("standalone client value");

            Assert.NotNull(encrypted);
            Assert.True(client.IsEncrypted(encrypted));
            Assert.Equal(unifiedName, client.GetUnifiedName(encrypted));
            Assert.Equal("standalone client value", client.Decrypt(encrypted));
        }

        [Fact]
        public void CryptoClient_DoesNotRequireGlobalCryptoConfig()
        {
            const string unifiedName = "config-independent-key";
            var keyRoot = Path.Combine(_testRoot, "config-independent-key-sets");
            WriteV2KeySet(keyRoot, unifiedName);
            B2BCryptoConfig.Override(null);

            var client = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = unifiedName
            });
            var encrypted = client.Encrypt("without global config");

            Assert.Equal("without global config", client.Decrypt(encrypted));
        }

        [Fact]
        public void CryptoClient_UsesExplicitActiveNameInsteadOfLatestName()
        {
            const string configuredName = "aaa-key";
            var keyRoot = Path.Combine(_testRoot, "key-sets");
            WriteV2KeySet(keyRoot, configuredName);
            WriteV2KeySet(keyRoot, "zzz-key");

            var client = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = configuredName
            });

            var encrypted = client.Encrypt("explicit active key");

            Assert.Equal(configuredName, client.GetUnifiedName(encrypted));
        }

        [Fact]
        public void CryptoClient_WithNoActiveNameRequiresExplicitEncryptionKey()
        {
            const string unifiedName = "explicit-key";
            var keyRoot = Path.Combine(_testRoot, "key-sets");
            WriteV2KeySet(keyRoot, unifiedName);
            var client = CryptoClient.Create(new CryptoOptions { KeyManagerBasePath = keyRoot });

            var exception = Assert.Throws<InvalidOperationException>(() => client.Encrypt("missing default"));
            Assert.Contains("ActiveUnifiedName", exception.Message);

            var encrypted = client.Encrypt("explicit value", unifiedName);

            Assert.Equal("explicit value", client.Decrypt(encrypted));
        }

        [Fact]
        public void CryptoClient_UnknownActiveNameFailsWhenEncryptionIsAttempted()
        {
            var client = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = Path.Combine(_testRoot, "missing-key-sets"),
                ActiveUnifiedName = "missing-key"
            });

            var exception = Assert.Throws<InvalidOperationException>(() => client.Encrypt("unknown active key"));

            Assert.Contains("找不到", exception.Message);
        }

        [Fact]
        public void CryptoClient_IsolatesDifferentKeyRoots()
        {
            const string unifiedName = "shared-name";
            var firstRoot = Path.Combine(_testRoot, "first");
            var secondRoot = Path.Combine(_testRoot, "second");
            WriteV2KeySet(firstRoot, unifiedName);
            WriteV2KeySet(secondRoot, unifiedName);

            var first = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = firstRoot,
                ActiveUnifiedName = unifiedName
            });
            var second = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = secondRoot,
                ActiveUnifiedName = unifiedName
            });

            var firstCipher = first.Encrypt("first context");
            var secondCipher = second.Encrypt("second context");

            Assert.Equal("first context", first.Decrypt(firstCipher));
            Assert.Equal("second context", second.Decrypt(secondCipher));
            Assert.Throws<CryptographicException>(() => second.Decrypt(firstCipher));
            Assert.Throws<CryptographicException>(() => first.Decrypt(secondCipher));
        }

        [Fact]
        public void Crypto_StaticFacadeIsThreadSafeAndRejectsUnsafeReinitialization()
        {
            const string unifiedName = "static-key";
            var keyRoot = Path.Combine(_testRoot, "static-key-sets");
            WriteV2KeySet(keyRoot, unifiedName);

            var beforeInitialize = Assert.Throws<InvalidOperationException>(() => Crypto.Encrypt("before initialize"));
            Assert.Contains("Crypto.Initialize", beforeInitialize.Message);

            Crypto.Initialize(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = unifiedName
            });

            // A normalized-equivalent configuration is idempotent.
            Crypto.Initialize(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot + Path.DirectorySeparatorChar,
                ActiveUnifiedName = unifiedName
            });

            var differentConfiguration = Assert.Throws<InvalidOperationException>(() => Crypto.Initialize(new CryptoOptions
            {
                KeyManagerBasePath = Path.Combine(_testRoot, "other-key-sets"),
                ActiveUnifiedName = unifiedName
            }));
            Assert.Contains("different configuration", differentConfiguration.Message);

            var encrypted = Crypto.Encrypt("static facade value");

            Assert.True(Crypto.IsValidEncryptedFormat(encrypted));
            Assert.Equal(unifiedName, Crypto.GetUnifiedName(encrypted));
            Assert.Equal("static facade value", Crypto.Decrypt(encrypted));
        }

        [Fact]
        public async Task Crypto_ConcurrentSameConfigurationInitializationIsIdempotent()
        {
            const string unifiedName = "concurrent-static-key";
            var keyRoot = Path.Combine(_testRoot, "concurrent-static-key-sets");
            WriteV2KeySet(keyRoot, unifiedName);

            await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Crypto.Initialize(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = unifiedName
            }))));

            Assert.Equal(unifiedName, Crypto.GetUnifiedName(Crypto.Encrypt("concurrent static value")));
        }

        [Fact]
        public async Task CryptoClient_SupportsConcurrentUse()
        {
            const string unifiedName = "concurrent-key";
            var keyRoot = Path.Combine(_testRoot, "concurrent-key-sets");
            WriteV2KeySet(keyRoot, unifiedName);
            var client = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = unifiedName
            });

            var encrypted = await Task.WhenAll(Enumerable.Range(0, 24)
                .Select(index => Task.Run(() => client.Encrypt("value-" + index))));
            var decrypted = await Task.WhenAll(encrypted
                .Select(value => Task.Run(() => client.Decrypt(value))));

            Assert.Equal(Enumerable.Range(0, 24).Select(index => "value-" + index), decrypted);
        }

        private static void WriteV2KeySet(string keyRoot, string unifiedName)
        {
            ConfigureKeyGeneration(keyRoot);

            var cryptoService = new CryptoService();
            var rsa = new RsaKeyGenerator().GenerateKeyOnly();
            var aes = new AesKeyGenerator().GenerateKeyOnly();
            var material = Encoding.UTF8.GetBytes(Convert.ToBase64String(aes.Key) + ":" + Convert.ToBase64String(aes.IV));
            var encryptedMaterial = cryptoService.Encrypt(material, CryptoAlgorithmType.RSA, rsa);
            var currentPath = Path.Combine(keyRoot, "current");

            Directory.CreateDirectory(currentPath);
            File.WriteAllBytes(Path.Combine(currentPath, unifiedName + ".aes"), encryptedMaterial);
            File.WriteAllText(Path.Combine(currentPath, unifiedName + ".pub"), rsa.PublicKey, Encoding.UTF8);
            File.WriteAllText(Path.Combine(currentPath, unifiedName + ".priv"), rsa.PrivateKey, Encoding.UTF8);
        }

        private static void ConfigureKeyGeneration(string keyRoot)
        {
            B2BCryptoConfig.Override(new CryptoConfigModel
            {
                KeyDirectory = keyRoot,
                RSA = new RsaConfig { KeySize = 2048 },
                ECC = new EccConfig { Curve = EccCurveType.NistP256 }
            });
        }
    }
}
