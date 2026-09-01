using System;

namespace B2B.CryptoLib.Models
{
    /// <summary>
    /// 描述同一統一名稱下 AES 與 RSA 金鑰檔案的位置。
    /// </summary>
    /// <remarks>
    /// 這個 DTO 由 <see cref="Services.KeyManagerService"/> 建立，代表已通過
    /// 完整性檔案存在檢查的 key set。<see cref="UsesLegacyMaterial"/> 用來區分
    /// <c>.der/.public.pem/.private.pem</c> 的舊版 material 解碼路徑與
    /// <c>.aes/.pub/.priv</c> 的 v2 路徑；它不會把金鑰 bytes 複製到此物件。
    /// </remarks>
    public class KeySetInfo
    {
        /// <summary>同一組 key set 的 unified name，不含副檔名。</summary>
        public string UnifiedName { get; set; } = null!;

        /// <summary>AES material 檔案的完整路徑。</summary>
        public string AesPath { get; set; } = null!;

        /// <summary>RSA public key PEM 檔案的完整路徑。</summary>
        public string RsaPublicKeyPath { get; set; } = null!;

        /// <summary>RSA private key PEM 檔案的完整路徑。</summary>
        public string RsaPrivateKeyPath { get; set; } = null!;

        /// <summary>金鑰組建立時間；目前查找路徑不會自動填入檔案時間，因此預設為 <see cref="DateTime"/> 的 default。</summary>
        public DateTime CreationTime
        {
            get; set;
        }

        /// <summary>
        /// 指出 .der 金鑰組是否採用舊版 PKCS#1 v1.5 與句點分隔的 AES 內容格式。
        /// </summary>
        /// <value><see langword="true"/> 表示使用 legacy RSA/材料解析；<see langword="false"/> 表示使用 v2 OAEP 與冒號分隔內容。</value>
        public bool UsesLegacyMaterial
        {
            get; set;
        }

        /// <summary>
        /// 取得此 key set 的 AES、RSA public 與 RSA private 檔案路徑。
        /// </summary>
        /// <returns>依 <see cref="AesPath"/>、<see cref="RsaPublicKeyPath"/>、<see cref="RsaPrivateKeyPath"/> 順序排列的新陣列。</returns>
        public string[] GetAllPaths() => new[] { AesPath, RsaPublicKeyPath, RsaPrivateKeyPath };
    }
}
