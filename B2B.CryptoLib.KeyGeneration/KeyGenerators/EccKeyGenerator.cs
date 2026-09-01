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
    /// <remarks>
    /// 曲線取自 <see cref="CryptoConfig.Current"/> 的 ECC 設定，目前支援 NIST P-256、
    /// P-384、P-521 與 secp256k1。PEM 輸出與曲線中繼資料會一併序列化；私鑰輸出
    /// 必須在離線受控環境保護，不應進入執行階段記錄或版本控制。
    /// </remarks>
    public class EccKeyGenerator : IKeyGenerator<EccKeyModel>
    {
        /// <summary>依目前 ECC 曲線設定產生一組新的金鑰對。</summary>
        /// <returns>含公開／私密 PEM、曲線與 UTC 建立時間的 <see cref="EccKeyModel"/>。</returns>
        /// <exception cref="InvalidOperationException"><see cref="CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="NotSupportedException">底層 Bouncy Castle 不支援設定的曲線。</exception>
        public EccKeyModel GenerateKeyOnly()
        {
            var curve = CryptoConfig.Current.ECC.Curve;
            var curveName = curve == EccCurveType.Secp256k1 ? "secp256k1" : curve == EccCurveType.NistP384 ? "P-384" : curve == EccCurveType.NistP521 ? "P-521" : "P-256";
            // 將設定的曲線與命名參數維持在一起；任一項變更都會影響簽章互通性
            // 與金鑰序列化格式。
            var parameters = curve == EccCurveType.Secp256k1 ? SecNamedCurves.GetByName(curveName) : NistNamedCurves.GetByName(curveName);
            var domainParameters = new ECDomainParameters(parameters.Curve, parameters.G, parameters.N, parameters.H, parameters.GetSeed());
            var generator = new ECKeyPairGenerator();

            generator.Init(new ECKeyGenerationParameters(domainParameters, new SecureRandom()));

            var pair = generator.GenerateKeyPair();

            return new EccKeyModel { PrivateKey = WritePem(pair.Private), PublicKey = WritePem(pair.Public), Curve = curve, CreatedAt = DateTime.UtcNow };
        }

        /// <summary>產生 ECC 金鑰對並以縮排 JSON 寫入 ECC 設定目錄。</summary>
        /// <param name="filePath">可選檔名或路徑；實作只使用其檔名，省略時產生八碼隨機 <c>.json</c> 名稱。</param>
        /// <returns>描述輸出檔名與完整路徑的 <see cref="KeyGenerationResult"/>。</returns>
        /// <exception cref="InvalidOperationException"><see cref="CryptoConfig.Current"/> 尚未設定。</exception>
        /// <exception cref="IOException">輸出目錄或檔案無法建立或寫入。</exception>
        /// <remarks>此方法不發布到執行階段的 <c>current</c>；輸出含私密金鑰，必須受控保存。</remarks>
        public KeyGenerationResult GenerateAndSaveKey(string? filePath = null)
        {
            var model = GenerateKeyOnly();
            var fileName = Path.GetFileName(filePath ?? CryptoConfig.GenerateKeyFileName(".json"));
            var path = CryptoConfig.GetKeyPath("ECC", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            File.WriteAllText(path, JsonConvert.SerializeObject(model, Formatting.Indented));

            return KeyGenerationResult.Create(CryptoAlgorithmType.ECC, fileName, path);
        }

        private static string WritePem(object value)
        {
            // 保留既有的 PEM 寫入器，讓產生的 .public.pem 與 .private.pem 檔案
            // 維持原本的標籤與金鑰配置。
            using (var writer = new StringWriter())
            {
                new PemWriter(writer).WriteObject(value);
                return writer.ToString();
            }
        }
    }
}
