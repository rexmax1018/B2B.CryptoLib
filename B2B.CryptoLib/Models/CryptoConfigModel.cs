using System;
using System.IO;
using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// AES 加密設定。
    /// </summary>
    /// <remarks>此模型保留舊版設定檔形狀；執行階段 GCM v2 目前固定以 AES 金鑰及 nonce（隨機數）處理資料。</remarks>
    public class AesConfig
    {
        /// <summary>要產生或使用的 AES 金鑰位元數；預設為 256。</summary>
        public int KeySize { get; set; } = 256;

        /// <summary>舊版設定所指定的文字編碼；預設為 UTF-8。</summary>
        public TextEncodingType Encoding { get; set; } = TextEncodingType.UTF8;
    }

    /// <summary>
    /// RSA 加密設定。
    /// </summary>
    /// <remarks>KeyGeneration 以此設定產生 RSA 金鑰；執行階段的 OAEP 路徑讀取已產生的 PEM 金鑰。</remarks>
    public class RsaConfig
    {
        /// <summary>要產生的 RSA 模數位元數；預設為 2048。</summary>
        public int KeySize { get; set; } = 2048;

        /// <summary>舊版設定所指定的文字編碼；預設為 UTF-8。</summary>
        public TextEncodingType Encoding { get; set; } = TextEncodingType.UTF8;

        /// <summary>RSA 金鑰在舊版設定目錄中的固定名稱。</summary>
        public string Directory => "RSA";
    }

    /// <summary>
    /// ECC 加密設定。
    /// </summary>
    /// <remarks>KeyGeneration 依 <see cref="Curve"/> 選擇曲線；簽章使用 SHA-256 搭配 ECDSA。</remarks>
    public class EccConfig
    {
        /// <summary>要產生的 ECC 曲線；預設為 <see cref="EccCurveType.NistP256"/>。</summary>
        public EccCurveType Curve { get; set; } = EccCurveType.NistP256;

        /// <summary>舊版設定所指定的文字編碼；預設為 UTF-8。</summary>
        public TextEncodingType Encoding { get; set; } = TextEncodingType.UTF8;

        /// <summary>ECC 金鑰在舊版設定目錄中的固定名稱。</summary>
        public string Directory => "ECC";
    }

    /// <summary>
    /// CryptoSuite 設定模型，包含金鑰目錄與各演算法參數。
    /// </summary>
    /// <remarks>
    /// 這個類別是 <see cref="B2B.CryptoLib.Config.CryptoConfig"/> 的相容資料模型，不等同於
    /// <see cref="CryptoOptions"/>。設定檔序列化時使用 <c>AES</c>、<c>RSA</c>、
    /// <c>ECC</c> 與 <c>UseUrlSafeBase64</c> 欄位；執行階段用戶端不會隱式讀取它。
    /// </remarks>
    public class CryptoConfigModel
    {
        private string _basePath = "Keys";

        /// <summary>
    /// 取得以應用程式基底目錄為基準解析後的金鑰目錄，或設定原始絕對／相對路徑。
        /// </summary>
    /// <value>絕對路徑會原樣使用；相對路徑會解析為 <see cref="AppDomain.CurrentDomain"/> 基底目錄下的完整路徑。</value>
    /// <remarks>設定器不會建立目錄；目錄建立由使用它的產生器或金鑰管理器負責。</remarks>
        public string KeyDirectory
        {
            get => Path.IsPathRooted(_basePath) ? _basePath : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _basePath));
            set => _basePath = value;
        }

        /// <summary>包含 AES 金鑰大小與相容文字編碼的設定。</summary>
        public AesConfig AES { get; set; } = new AesConfig();

        /// <summary>包含 RSA 金鑰大小與相容文字編碼的設定。</summary>
        public RsaConfig RSA { get; set; } = new RsaConfig();

        /// <summary>包含 ECC 曲線與相容文字編碼的設定。</summary>
        public EccConfig ECC { get; set; } = new EccConfig();

        /// <summary>保留舊版 URL-safe Base64 選項；是否套用由相應舊版呼叫流程決定。</summary>
        public bool UseUrlSafeBase64 { get; set; } = true;
    }
}
