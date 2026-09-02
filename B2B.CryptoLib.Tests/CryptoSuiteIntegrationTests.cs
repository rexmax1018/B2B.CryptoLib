using Autofac;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.KeyGeneration;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Models;
using B2B.CryptoLib.Models;
using B2B.CryptoLib.Services;
using System;
using System.IO;
using CryptographicException = System.Security.Cryptography.CryptographicException;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace B2B.CryptoLib.Tests
{
    public class CryptoSuiteIntegrationTests : IDisposable
    {
        private string _testRoot;
        private IContainer _container;

        public CryptoSuiteIntegrationTests()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "B2B.CryptoLib.Tests", Guid.NewGuid().ToString("N"));

            CryptoConfig.Override(new CryptoConfigModel
            {
                KeyDirectory = Path.Combine(_testRoot, "generated-keys"),
                RSA = new RsaConfig { KeySize = 2048 },
                ECC = new EccConfig { Curve = EccCurveType.NistP256 }
            });

            var builder = new ContainerBuilder();

            builder.RegisterModule(new CryptoSuiteModule(Path.Combine(_testRoot, "key-sets")));
            builder.RegisterModule(new KeyGenerationModule());

            _container = builder.Build();
        }

        public void Dispose()
        {
            _container?.Dispose();

            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [Fact]
        public void DependencyInjection_ResolvesAllPublicServices()
        {
            Assert.NotNull(_container.Resolve<ICryptoKeyService>());
            Assert.NotNull(_container.Resolve<IKeyGenerationService>());
            Assert.NotNull(_container.Resolve<IKeySetGenerationService>());
            Assert.NotNull(_container.Resolve<ICryptoService>());
            Assert.NotNull(_container.Resolve<IDataEncryptionService>());
            Assert.NotNull(_container.Resolve<KeyManagerService>());
            Assert.NotNull(_container.Resolve<ICryptoClient>());
        }

        [Fact]
        public void KeyService_GeneratesAndLoadsAesRsaAndEccKeys()
        {
            var keyService = _container.Resolve<ICryptoKeyService>();
            var keyGeneration = _container.Resolve<IKeyGenerationService>();

            var aes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);
            var rsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var ecc = keyGeneration.GenerateKeyOnly<EccKeyModel>(CryptoAlgorithmType.ECC);

            Assert.Equal(32, aes.Key.Length);
            Assert.Equal(16, aes.IV.Length);
            Assert.Contains("BEGIN PUBLIC KEY", rsa.PublicKey);
            Assert.Contains("BEGIN RSA PRIVATE KEY", rsa.PrivateKey);
            Assert.Equal(2048, rsa.KeySize);
            Assert.Contains("BEGIN PUBLIC KEY", ecc.PublicKey);
            Assert.Contains("BEGIN EC PRIVATE KEY", ecc.PrivateKey);
            Assert.Equal(EccCurveType.NistP256, ecc.Curve);

            var saved = keyGeneration.GenerateAndSaveKey<SymmetricKeyModel>(CryptoAlgorithmType.AES, "integration-aes.json");

            Assert.True(File.Exists(saved.KeyFilePath));

            var loaded = keyService.LoadFromFile<SymmetricKeyModel>(CryptoAlgorithmType.AES, saved.KeyFilePath);

            Assert.Equal(32, loaded.Key.Length);
            Assert.Equal(16, loaded.IV.Length);
        }

        [Fact]
        public async Task KeySetGenerationService_UsesGeneratedPrefixAndLegacyFileNames()
        {
            var keySetRoot = Path.Combine(_testRoot, "key-sets");
            CryptoConfig.Override(new CryptoConfigModel
            {
                KeyDirectory = keySetRoot,
                RSA = new RsaConfig { KeySize = 2048 },
                ECC = new EccConfig { Curve = EccCurveType.NistP256 }
            });

            var keySetGeneration = _container.Resolve<IKeySetGenerationService>();
            var keyManager = _container.Resolve<KeyManagerService>();
            var dataEncryption = _container.Resolve<IDataEncryptionService>();
            var result = keySetGeneration.GenerateAndSave();

            Assert.Matches("^[a-zA-Z0-9]{8}$", result.UnifiedName);
            Assert.EndsWith(result.UnifiedName + ".der", result.AesKeyPath);
            Assert.EndsWith(result.UnifiedName + ".public.pem", result.PublicKeyPath);
            Assert.EndsWith(result.UnifiedName + ".private.pem", result.PrivateKeyPath);
            Assert.True(File.Exists(result.AesKeyPath));
            Assert.True(File.Exists(result.PublicKeyPath));
            Assert.True(File.Exists(result.PrivateKeyPath));
            Assert.StartsWith("-----BEGIN PUBLIC KEY-----", File.ReadAllText(result.PublicKeyPath, Encoding.UTF8));
            Assert.StartsWith("-----BEGIN RSA PRIVATE KEY-----", File.ReadAllText(result.PrivateKeyPath, Encoding.UTF8));

            var generatedRsa = new RsaKeyModel
            {
                PublicKey = File.ReadAllText(result.PublicKeyPath, Encoding.UTF8),
                PrivateKey = File.ReadAllText(result.PrivateKeyPath, Encoding.UTF8)
            };
            var newMaterial = Encoding.UTF8.GetString(_container.Resolve<ICryptoService>().Decrypt(File.ReadAllBytes(result.AesKeyPath), CryptoAlgorithmType.RSA, generatedRsa));

            Assert.Equal(2, newMaterial.Split(':').Length);
            Assert.DoesNotContain(".", newMaterial);

            await keyManager.StartAsync();

            var encrypted = dataEncryption.Encrypt("由 KEYSET 指令產生的金鑰", result.UnifiedName);
            Assert.Equal(result.UnifiedName, keyManager.GetLatestActiveUnifiedName());
            Assert.Equal("由 KEYSET 指令產生的金鑰", dataEncryption.Decrypt(encrypted));
        }

        [Fact]
        public void CryptoService_RoundTripsDataAndVerifiesRsaAndEccSignatures()
        {
            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var cryptoService = _container.Resolve<ICryptoService>();
            var data = Encoding.UTF8.GetBytes("B2B.CryptoLib integration test payload");

            var aes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);
            var aesEncrypted = cryptoService.Encrypt(data, CryptoAlgorithmType.AES, aes);

            Assert.Equal(data, cryptoService.Decrypt(aesEncrypted, CryptoAlgorithmType.AES, aes));

            var rsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var rsaEncrypted = cryptoService.Encrypt(data, CryptoAlgorithmType.RSA, rsa);

            Assert.Equal(data, cryptoService.Decrypt(rsaEncrypted, CryptoAlgorithmType.RSA, rsa));

            var rsaSignature = cryptoService.Sign(data, CryptoAlgorithmType.RSA, rsa);

            Assert.True(cryptoService.Verify(data, rsaSignature, CryptoAlgorithmType.RSA, rsa));

            var ecc = keyGeneration.GenerateKeyOnly<EccKeyModel>(CryptoAlgorithmType.ECC);
            var eccSignature = cryptoService.Sign(data, CryptoAlgorithmType.ECC, ecc);

            Assert.True(cryptoService.Verify(data, eccSignature, CryptoAlgorithmType.ECC, ecc));
        }

        [Fact]
        public async Task DataEncryptionService_EncryptsAndDecryptsUsingDiManagedKeySet()
        {
            const string unifiedName = "20260713A";
            const string plainText = "由 DI 管理的 AES 加密內容";

            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var cryptoService = _container.Resolve<ICryptoService>();
            var keyManager = _container.Resolve<KeyManagerService>();
            var dataEncryption = _container.Resolve<IDataEncryptionService>();
            var rsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var aes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);

            WriteKeySetToUpdateFolder(unifiedName, rsa, aes, cryptoService);

            await keyManager.StartAsync();

            var encrypted = dataEncryption.Encrypt(plainText, unifiedName);

            Assert.True(dataEncryption.IsValidEncryptedFormat(encrypted));
            Assert.Equal(unifiedName, dataEncryption.GetUnifiedNameFromEncryptedData(encrypted));
            Assert.Equal(unifiedName, keyManager.GetLatestActiveUnifiedName());
            Assert.Equal(plainText, dataEncryption.Decrypt(encrypted));
        }

        [Fact]
        public async Task DataEncryptionService_UsesGcmWithoutChangingOuterFormat()
        {
            const string unifiedName = "20260713B";
            const string plainText = "新版 GCM 加密內容";

            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var cryptoService = _container.Resolve<ICryptoService>();
            var keyManager = _container.Resolve<KeyManagerService>();
            var dataEncryption = _container.Resolve<IDataEncryptionService>();
            var rsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var aes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);

            WriteKeySetToUpdateFolder(unifiedName, rsa, aes, cryptoService);
            await keyManager.StartAsync();

            var first = dataEncryption.Encrypt(plainText, unifiedName)!;
            var second = dataEncryption.Encrypt(plainText, unifiedName)!;
            var separator = first.LastIndexOf('.');

            Assert.NotEqual(first, second);
            Assert.Equal(unifiedName, first.Substring(separator + 1));
            Assert.DoesNotContain(".", first.Substring(0, separator));
            var payload = Convert.FromBase64String(first.Substring(0, separator));
            var magic = Encoding.ASCII.GetBytes("B2BCGCM");

            Assert.Equal(magic, payload.Take(magic.Length).ToArray());
            Assert.Equal((byte)2, payload[magic.Length]);
            Assert.Equal(Encoding.UTF8.GetByteCount(plainText) + 16, payload.Length - magic.Length - 1 - 12);
            Assert.Equal(plainText, dataEncryption.Decrypt(first));
            Assert.Equal(plainText, dataEncryption.Decrypt(second));
        }

        [Fact]
        public async Task DataEncryptionService_RejectsUnsupportedGcmVersion()
        {
            const string unifiedName = "20260713Version";

            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var cryptoService = _container.Resolve<ICryptoService>();
            var keyManager = _container.Resolve<KeyManagerService>();
            var dataEncryption = _container.Resolve<IDataEncryptionService>();
            var rsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var aes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);

            WriteKeySetToUpdateFolder(unifiedName, rsa, aes, cryptoService);
            await keyManager.StartAsync();

            var encrypted = dataEncryption.Encrypt("版本測試", unifiedName)!;
            var separator = encrypted.LastIndexOf('.');
            var payload = Convert.FromBase64String(encrypted.Substring(0, separator));
            payload["B2BCGCM".Length] = 3;
            var tampered = Convert.ToBase64String(payload) + encrypted.Substring(separator);

            Assert.Throws<CryptographicException>(() => dataEncryption.Decrypt(tampered));
        }

        [Fact]
        public async Task DataEncryptionService_DecryptsLegacyCbcPayloadWithExistingKeySet()
        {
            const string unifiedName = "20260713C";
            const string plainText = "既有 CBC 密文";

            var cryptoService = _container.Resolve<ICryptoService>();
            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var rsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var aes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);
            var keySetRoot = Path.Combine(_testRoot, "key-sets");
            WriteLegacyKeySetToHistory(unifiedName, rsa, aes, keySetRoot);

            var legacyManager = new KeyManagerService(keySetRoot, cryptoService);
            var legacyEncryption = new DataEncryptionService(cryptoService, legacyManager);
            var legacyCipher = cryptoService.Encrypt(Encoding.UTF8.GetBytes(plainText), CryptoAlgorithmType.AES, aes);
            var legacyValue = Convert.ToBase64String(legacyCipher) + "." + unifiedName;

            Assert.Equal(plainText, legacyEncryption.Decrypt(legacyValue));
        }

        [Fact]
        public async Task DataEncryptionService_RejectsTamperedGcmPayload()
        {
            const string unifiedName = "20260713D";

            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var cryptoService = _container.Resolve<ICryptoService>();
            var keyManager = _container.Resolve<KeyManagerService>();
            var dataEncryption = _container.Resolve<IDataEncryptionService>();
            var rsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var aes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);

            WriteKeySetToUpdateFolder(unifiedName, rsa, aes, cryptoService);
            await keyManager.StartAsync();

            var encrypted = dataEncryption.Encrypt("不可被竄改", unifiedName)!;
            var separator = encrypted.LastIndexOf('.');
            var payload = Convert.FromBase64String(encrypted.Substring(0, separator));
            payload[payload.Length - 1] ^= 0x01;
            var tampered = Convert.ToBase64String(payload) + encrypted.Substring(separator);

            Assert.Throws<CryptographicException>(() => dataEncryption.Decrypt(tampered));
        }

        [Fact]
        public async Task KeyManagerService_DoesNotPublishIncompleteKeySet()
        {
            const string unifiedName = "20260713E";

            var keyManager = _container.Resolve<KeyManagerService>();
            var updatePath = Path.Combine(_testRoot, "key-sets", "update");
            var incompleteAesPath = Path.Combine(updatePath, unifiedName + ".aes");

            File.WriteAllBytes(incompleteAesPath, new byte[] { 1, 2, 3 });
            await keyManager.StartAsync();

            Assert.True(File.Exists(incompleteAesPath));
            Assert.False(File.Exists(Path.Combine(_testRoot, "key-sets", "current", unifiedName + ".aes")));
            Assert.Throws<InvalidOperationException>(() => keyManager.GetLatestActiveUnifiedName());
        }

        [Fact]
        public async Task KeyManagerService_LoadsExistingKeySetsFromHistory()
        {
            const string unifiedName = "20260713F";
            const string plainText = "歷史金鑰仍可使用";

            var cryptoService = _container.Resolve<ICryptoService>();
            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var rsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var aes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);
            var keySetRoot = Path.Combine(_testRoot, "key-sets");
            WriteLegacyKeySetToHistory(unifiedName, rsa, aes, keySetRoot);

            var historyManager = new KeyManagerService(keySetRoot, cryptoService);
            var historyEncryption = new DataEncryptionService(cryptoService, historyManager);
            var loadedAes = historyManager.GetAesKey(unifiedName);
            var encrypted = Convert.ToBase64String(cryptoService.Encrypt(Encoding.UTF8.GetBytes(plainText), CryptoAlgorithmType.AES, loadedAes)) + "." + unifiedName;

            Assert.Equal(aes.Key, loadedAes.Key);
            Assert.Equal(plainText, historyEncryption.Decrypt(encrypted));
        }

        [Fact]
        public async Task DataEncryptionService_DecryptsOldAndNewKeyVersionsAfterRotation()
        {
            const string oldUnifiedName = "20260713G";
            const string newUnifiedName = "20260713H";
            const string legacyPlainText = "舊版 CBC 金鑰資料";
            const string oldGcmPlainText = "舊版金鑰產生的 GCM 資料";
            const string newGcmPlainText = "新版金鑰產生的 GCM 資料";

            var cryptoService = _container.Resolve<ICryptoService>();
            var keyManager = _container.Resolve<KeyManagerService>();
            var dataEncryption = _container.Resolve<IDataEncryptionService>();
            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var oldRsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var oldAes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);
            var keySetRoot = Path.Combine(_testRoot, "key-sets");
            WriteLegacyKeySetToHistory(oldUnifiedName, oldRsa, oldAes, keySetRoot);
            var legacyValue = Convert.ToBase64String(cryptoService.Encrypt(Encoding.UTF8.GetBytes(legacyPlainText), CryptoAlgorithmType.AES, oldAes)) + "." + oldUnifiedName;
            var oldGcmValue = dataEncryption.Encrypt(oldGcmPlainText, oldUnifiedName)!;

            CryptoConfig.Override(new CryptoConfigModel
            {
                KeyDirectory = keySetRoot,
                RSA = new RsaConfig { KeySize = 2048 },
                ECC = new EccConfig { Curve = EccCurveType.NistP256 }
            });
            var keySetGeneration = _container.Resolve<IKeySetGenerationService>();
            keySetGeneration.GenerateAndSave(newUnifiedName);
            await keyManager.StartAsync();

            var newGcmValue = dataEncryption.Encrypt(newGcmPlainText, newUnifiedName)!;
            var oldValueWithNewKeyName = oldGcmValue.Substring(0, oldGcmValue.LastIndexOf('.') + 1) + newUnifiedName;

            Assert.Equal(newUnifiedName, keyManager.GetLatestActiveUnifiedName());
            Assert.Equal(legacyPlainText, dataEncryption.Decrypt(legacyValue));
            Assert.Equal(oldGcmPlainText, dataEncryption.Decrypt(oldGcmValue));
            Assert.Equal(newGcmPlainText, dataEncryption.Decrypt(newGcmValue));
            Assert.Throws<CryptographicException>(() => dataEncryption.Decrypt(oldValueWithNewKeyName));
        }

        [Fact]
        public async Task DataEncryptionService_CrossDecryptsNewAndLegacyKeySets()
        {
            const string legacyUnifiedName = "legacyCrossA";
            const string newUnifiedName = "newCrossB";
            const string legacyCbcPlainText = "由舊版系統以 CBC 寫入的資料";
            const string legacyKeyGcmPlainText = "以舊版金鑰由新版程式寫入的 GCM 資料";
            const string newKeyGcmPlainText = "以新版金鑰寫入的 GCM 資料";

            var keySetRoot = Path.Combine(_testRoot, "key-sets");
            var cryptoService = _container.Resolve<ICryptoService>();
            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var keyManager = _container.Resolve<KeyManagerService>();
            var keySetGeneration = _container.Resolve<IKeySetGenerationService>();
            var legacyRsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var legacyAes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);

            WriteLegacyKeySetToHistory(legacyUnifiedName, legacyRsa, legacyAes, keySetRoot);
            CryptoConfig.Override(new CryptoConfigModel
            {
                KeyDirectory = keySetRoot,
                RSA = new RsaConfig { KeySize = 2048 },
                ECC = new EccConfig { Curve = EccCurveType.NistP256 }
            });
            keySetGeneration.GenerateAndSave(newUnifiedName);
            await keyManager.StartAsync();

            var writer = new DataEncryptionService(cryptoService, keyManager);
            var legacyCbcCipher = Convert.ToBase64String(cryptoService.Encrypt(Encoding.UTF8.GetBytes(legacyCbcPlainText), CryptoAlgorithmType.AES, legacyAes)) + "." + legacyUnifiedName;
            var legacyKeyGcmCipher = writer.Encrypt(legacyKeyGcmPlainText, legacyUnifiedName)!;
            var newKeyGcmCipher = writer.Encrypt(newKeyGcmPlainText, newUnifiedName)!;

            // 使用新的服務執行個體，確保資料會從 current/history 重新載入而非沿用寫入端快取。
            var reader = new DataEncryptionService(cryptoService, new KeyManagerService(keySetRoot, cryptoService));

            Assert.Equal(legacyCbcPlainText, reader.Decrypt(legacyCbcCipher));
            Assert.Equal(legacyKeyGcmPlainText, reader.Decrypt(legacyKeyGcmCipher));
            Assert.Equal(newKeyGcmPlainText, reader.Decrypt(newKeyGcmCipher));

            var legacyCipherWithNewName = legacyKeyGcmCipher.Substring(0, legacyKeyGcmCipher.LastIndexOf('.') + 1) + newUnifiedName;
            var newCipherWithLegacyName = newKeyGcmCipher.Substring(0, newKeyGcmCipher.LastIndexOf('.') + 1) + legacyUnifiedName;

            Assert.Throws<CryptographicException>(() => reader.Decrypt(legacyCipherWithNewName));
            Assert.Throws<CryptographicException>(() => reader.Decrypt(newCipherWithLegacyName));
        }

        [Fact]
        public async Task KeyManagerService_ClearsCacheWhenReplacingSameUnifiedName()
        {
            const string unifiedName = "cache-rotate";

            var cryptoService = _container.Resolve<ICryptoService>();
            var keyGeneration = _container.Resolve<IKeyGenerationService>();
            var keyManager = _container.Resolve<KeyManagerService>();
            var oldRsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var oldAes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);

            WriteKeySetToUpdateFolder(unifiedName, oldRsa, oldAes, cryptoService);
            await keyManager.StartAsync();
            var cached = keyManager.GetAesKey(unifiedName);

            var newRsa = keyGeneration.GenerateKeyOnly<RsaKeyModel>(CryptoAlgorithmType.RSA);
            var newAes = keyGeneration.GenerateKeyOnly<SymmetricKeyModel>(CryptoAlgorithmType.AES);

            WriteKeySetToUpdateFolder(unifiedName, newRsa, newAes, cryptoService);
            await keyManager.StartAsync();
            var rotated = keyManager.GetAesKey(unifiedName);

            Assert.Equal(oldAes.Key, cached.Key);
            Assert.Equal(newAes.Key, rotated.Key);
            Assert.NotEqual(cached.Key, rotated.Key);
        }

        private void WriteKeySetToUpdateFolder(string unifiedName, RsaKeyModel rsa, SymmetricKeyModel aes, ICryptoService cryptoService)
        {
            var updatePath = Path.Combine(_testRoot, "key-sets", "update");
            var aesMaterial = string.Concat(Convert.ToBase64String(aes.Key), ":", Convert.ToBase64String(aes.IV));
            var encryptedAesMaterial = cryptoService.Encrypt(Encoding.UTF8.GetBytes(aesMaterial), CryptoAlgorithmType.RSA, rsa);

            File.WriteAllBytes(Path.Combine(updatePath, unifiedName + ".aes"), encryptedAesMaterial);
            File.WriteAllText(Path.Combine(updatePath, unifiedName + ".pub"), rsa.PublicKey, Encoding.UTF8);
            File.WriteAllText(Path.Combine(updatePath, unifiedName + ".priv"), rsa.PrivateKey, Encoding.UTF8);
        }

        private static void WriteLegacyKeySetToHistory(string unifiedName, RsaKeyModel rsa, SymmetricKeyModel aes, string keySetRoot)
        {
            var historyPath = Path.Combine(keySetRoot, "history");
            var material = string.Concat(Convert.ToBase64String(aes.Key), ".", Convert.ToBase64String(aes.IV));
            var encryptedMaterial = LegacyKeySetCrypto.Encrypt(Encoding.UTF8.GetBytes(material), rsa);

            Directory.CreateDirectory(historyPath);
            File.WriteAllBytes(Path.Combine(historyPath, unifiedName + ".der"), encryptedMaterial);
            File.WriteAllText(Path.Combine(historyPath, unifiedName + ".public.pem"), rsa.PublicKey, Encoding.UTF8);
            File.WriteAllText(Path.Combine(historyPath, unifiedName + ".private.pem"), rsa.PrivateKey, Encoding.UTF8);
        }
    }
}
