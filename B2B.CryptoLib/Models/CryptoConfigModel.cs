using System;
using System.IO;
using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// AES 加密設定。
    /// </summary>
    public class AesConfig
    {
        public int KeySize { get; set; } = 256;

        public TextEncodingType Encoding { get; set; } = TextEncodingType.UTF8;
    }

    /// <summary>
    /// RSA 加密設定。
    /// </summary>
    public class RsaConfig
    {
        public int KeySize { get; set; } = 2048;

        public TextEncodingType Encoding { get; set; } = TextEncodingType.UTF8;

        public string Directory => "RSA";
    }

    /// <summary>
    /// ECC 加密設定。
    /// </summary>
    public class EccConfig
    {
        public EccCurveType Curve { get; set; } = EccCurveType.NistP256;

        public TextEncodingType Encoding { get; set; } = TextEncodingType.UTF8;

        public string Directory => "ECC";
    }

    /// <summary>
    /// CryptoSuite 設定模型，包含金鑰目錄與各演算法參數。
    /// </summary>
    public class CryptoConfigModel
    {
        private string _basePath = "Keys";

        public string KeyDirectory
        {
            get => Path.IsPathRooted(_basePath) ? _basePath : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _basePath));
            set => _basePath = value;
        }

        public AesConfig AES { get; set; } = new AesConfig();

        public RsaConfig RSA { get; set; } = new RsaConfig();

        public EccConfig ECC { get; set; } = new EccConfig();

        public bool UseUrlSafeBase64 { get; set; } = true;
    }
}
