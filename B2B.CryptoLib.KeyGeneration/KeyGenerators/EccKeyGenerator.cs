using System;
using System.IO;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Models;
using B2B.CryptoLib.Models;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace B2B.CryptoLib.KeyGeneration.KeyGenerators
{
    /// <summary>
    /// 離線 ECC 金鑰產生器，依設定的橢圓曲線產生 PEM 格式的金鑰組。
    /// </summary>
    public class EccKeyGenerator : IKeyGenerator<EccKeyModel>
    {
        public EccKeyModel GenerateKeyOnly()
        {
            var curve = CryptoConfig.Current.ECC.Curve;
            var curveName = curve == EccCurveType.Secp256k1 ? "secp256k1" : curve == EccCurveType.NistP384 ? "P-384" : curve == EccCurveType.NistP521 ? "P-521" : "P-256";
            var parameters = curve == EccCurveType.Secp256k1 ? SecNamedCurves.GetByName(curveName) : NistNamedCurves.GetByName(curveName);
            var domainParameters = new ECDomainParameters(parameters.Curve, parameters.G, parameters.N, parameters.H, parameters.GetSeed());
            var generator = new ECKeyPairGenerator();

            generator.Init(new ECKeyGenerationParameters(domainParameters, new SecureRandom()));

            var pair = generator.GenerateKeyPair();

            return new EccKeyModel { PrivateKey = WritePem(pair.Private), PublicKey = WritePem(pair.Public), Curve = curve, CreatedAt = DateTime.UtcNow };
        }

        public KeyGenerationResult GenerateAndSaveKey(string filePath = null)
        {
            var model = GenerateKeyOnly();
            var fileName = Path.GetFileName(filePath ?? CryptoConfig.GenerateKeyFileName(".json"));
            var path = CryptoConfig.GetKeyPath("ECC", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllText(path, JsonConvert.SerializeObject(model, Formatting.Indented));

            return KeyGenerationResult.Create(CryptoAlgorithmType.ECC, fileName, path);
        }

        private static string WritePem(object value)
        {
            using (var writer = new StringWriter())
            {
                new PemWriter(writer).WriteObject(value);
                return writer.ToString();
            }
        }
    }
}