using Autofac;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.KeyGeneration;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.Models;
using B2B.CryptoLib.Services;
using System;
using System.IO;
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
            Assert.NotNull(_container.Resolve<ICryptoService>());
            Assert.NotNull(_container.Resolve<IDataEncryptionService>());
            Assert.NotNull(_container.Resolve<KeyManagerService>());
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

        private void WriteKeySetToUpdateFolder(string unifiedName, RsaKeyModel rsa, SymmetricKeyModel aes, ICryptoService cryptoService)
        {
            var updatePath = Path.Combine(_testRoot, "key-sets", "update");
            var aesMaterial = string.Concat(Convert.ToBase64String(aes.Key), ":", Convert.ToBase64String(aes.IV));
            var encryptedAesMaterial = cryptoService.Encrypt(Encoding.UTF8.GetBytes(aesMaterial), CryptoAlgorithmType.RSA, rsa);

            File.WriteAllBytes(Path.Combine(updatePath, unifiedName + ".aes"), encryptedAesMaterial);
            File.WriteAllText(Path.Combine(updatePath, unifiedName + ".pub"), rsa.PublicKey, Encoding.UTF8);
            File.WriteAllText(Path.Combine(updatePath, unifiedName + ".priv"), rsa.PrivateKey, Encoding.UTF8);
        }
    }
}