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
            Assert.True(client.IsValidEncryptedFormat(encrypted));
            Assert.Equal(unifiedName, client.GetUnifiedName(encrypted));
            Assert.Equal("standalone client value", client.Decrypt(encrypted));
        }

        [Fact]
        public void CryptoClient_CreateDoesNotConsumeUpdateAndReadsCurrentAndHistory()
        {
            const string currentName = "active-current";
            const string historyName = "historical-key";
            const string pendingName = "pending-key";
            var keyRoot = Path.Combine(_testRoot, "side-effect-free-key-sets");

            WriteV2KeySet(keyRoot, currentName);
            WriteV2KeySet(keyRoot, historyName, "history");
            WriteV2KeySet(keyRoot, pendingName, "update");

            var client = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = currentName
            });

            Assert.True(File.Exists(Path.Combine(keyRoot, "update", pendingName + ".aes")));
            Assert.True(File.Exists(Path.Combine(keyRoot, "update", pendingName + ".pub")));
            Assert.True(File.Exists(Path.Combine(keyRoot, "update", pendingName + ".priv")));
            Assert.False(File.Exists(Path.Combine(keyRoot, "current", pendingName + ".aes")));

            var currentCipher = client.Encrypt("current value");
            var historyCipher = client.Encrypt("history value", historyName);

            Assert.Equal("current value", client.Decrypt(currentCipher));
            Assert.Equal("history value", client.Decrypt(historyCipher));
        }

        [Fact]
        public async Task CryptoClient_ExplicitUpdatePublishesPendingKeySet()
        {
            const string unifiedName = "explicit-client-update";
            var keyRoot = Path.Combine(_testRoot, "explicit-client-update-key-sets");
            WriteV2KeySet(keyRoot, unifiedName, "update");

            var client = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = unifiedName
            });

            Assert.True(File.Exists(Path.Combine(keyRoot, "update", unifiedName + ".aes")));

            await client.UpdateKeySetsAsync();

            Assert.False(File.Exists(Path.Combine(keyRoot, "update", unifiedName + ".aes")));
            Assert.True(File.Exists(Path.Combine(keyRoot, "current", unifiedName + ".aes")));
            Assert.Equal("explicit update value", client.Decrypt(client.Encrypt("explicit update value")));
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
        public void CryptoOptions_PathComparisonFollowsRuntimePlatform()
        {
            var upperCasePath = new CryptoOptions
            {
                KeyManagerBasePath = Path.Combine(_testRoot, "CaseSensitiveRoot"),
                ActiveUnifiedName = "same-key"
            }.Normalize();
            var lowerCasePath = new CryptoOptions
            {
                KeyManagerBasePath = Path.Combine(_testRoot, "casesensitiveroot"),
                ActiveUnifiedName = "same-key"
            }.Normalize();
            var equivalent = CryptoOptions.AreEquivalent(upperCasePath, lowerCasePath);

            if (OperatingSystem.IsWindows())
                Assert.True(equivalent);
            else
                Assert.False(equivalent);

            var trailingSeparatorPath = new CryptoOptions
            {
                KeyManagerBasePath = upperCasePath.KeyManagerBasePath + Path.DirectorySeparatorChar,
                ActiveUnifiedName = "same-key"
            }.Normalize();

            Assert.True(CryptoOptions.AreEquivalent(upperCasePath, trailingSeparatorPath));
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

            // 正規化後相等的設定具備冪等性。
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

            var decrypted = await Task.WhenAll(Enumerable.Range(0, 24).Select(index => Task.Run(() =>
            {
                var encrypted = Crypto.Encrypt("concurrent static value-" + index);
                return Crypto.Decrypt(encrypted);
            })));

            Assert.Equal(Enumerable.Range(0, 24).Select(index => "concurrent static value-" + index), decrypted);
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

        [Fact]
        public async Task CryptoClient_SameRootRequiresUpdateCoordination()
        {
            const string unifiedName = "same-root-key";
            var keyRoot = Path.Combine(_testRoot, "same-root-key-sets");
            WriteV2KeySet(keyRoot, unifiedName);

            var first = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = unifiedName
            });
            var second = CryptoClient.Create(new CryptoOptions
            {
                KeyManagerBasePath = keyRoot,
                ActiveUnifiedName = unifiedName
            });

            // 先以原始金鑰填入第二個用戶端的快取。
            Assert.Equal("before rotation", second.Decrypt(second.Encrypt("before rotation")));

            WriteV2KeySet(keyRoot, unifiedName, "update");
            await first.UpdateKeySetsAsync();

            var rotatedCipher = first.Encrypt("after rotation");

            Assert.Equal("after rotation", first.Decrypt(rotatedCipher));
            Assert.Throws<CryptographicException>(() => second.Decrypt(rotatedCipher));
        }

        [Fact]
        public async Task KeyManagerService_ExplicitStartConsumesPendingKeySet()
        {
            const string unifiedName = "explicit-manager-update";
            var keyRoot = Path.Combine(_testRoot, "explicit-manager-update-key-sets");
            WriteV2KeySet(keyRoot, unifiedName, "update");

            var cryptoService = new CryptoService();
            var keyManager = new KeyManagerService(keyRoot, cryptoService);

            await keyManager.StartAsync();

            Assert.False(File.Exists(Path.Combine(keyRoot, "update", unifiedName + ".aes")));
            Assert.False(File.Exists(Path.Combine(keyRoot, "update", unifiedName + ".pub")));
            Assert.False(File.Exists(Path.Combine(keyRoot, "update", unifiedName + ".priv")));
            Assert.Equal(unifiedName, keyManager.GetLatestActiveUnifiedName());

            var dataEncryption = new DataEncryptionService(cryptoService, keyManager);
            var encrypted = dataEncryption.Encrypt("explicit manager value", unifiedName);

            Assert.Equal("explicit manager value", dataEncryption.Decrypt(encrypted));
        }

        private static void WriteV2KeySet(string keyRoot, string unifiedName, string folder = "current")
        {
            ConfigureKeyGeneration(keyRoot);

            var cryptoService = new CryptoService();
            var rsa = new RsaKeyGenerator().GenerateKeyOnly();
            var aes = new AesKeyGenerator().GenerateKeyOnly();
            var material = Encoding.UTF8.GetBytes(Convert.ToBase64String(aes.Key) + ":" + Convert.ToBase64String(aes.IV));
            var encryptedMaterial = cryptoService.Encrypt(material, CryptoAlgorithmType.RSA, rsa);
            var currentPath = Path.Combine(keyRoot, folder);

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
