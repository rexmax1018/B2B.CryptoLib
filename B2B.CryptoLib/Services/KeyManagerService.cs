using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.Helpers;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib.Services
{
    /// <summary>
    /// 管理 Current、History 與 Update 目錄中的金鑰組，並提供記憶體快取。
    /// 同時支援新版 .aes/.pub/.priv 與舊版 .der/.public.pem/.private.pem 檔名及內容格式。
    /// </summary>
    public class KeyManagerService
    {
        private static readonly KeySetLayout[] KeySetLayouts =
        {
            new KeySetLayout("v2", ".aes", ".pub", ".priv", false),
            new KeySetLayout("legacy", ".der", ".public.pem", ".private.pem", true)
        };

        private readonly string _basePath;
        private readonly string _updatePath;
        private readonly string _currentPath;
        private readonly string _historyPath;
        private readonly ICryptoService _cryptoService;
        private readonly object _keySetGate = new object();

        private readonly ConcurrentDictionary<string, Lazy<RsaKeyModel>> _rsaCache = new ConcurrentDictionary<string, Lazy<RsaKeyModel>>();
        private readonly ConcurrentDictionary<string, Lazy<SymmetricKeyModel>> _aesCache = new ConcurrentDictionary<string, Lazy<SymmetricKeyModel>>();

        public KeyManagerService(string basePath, ICryptoService cryptoService)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
            _cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));

            _updatePath = Path.Combine(_basePath, "update");
            _currentPath = Path.Combine(_basePath, "current");
            _historyPath = Path.Combine(_basePath, "history");

            Directory.CreateDirectory(_basePath);
            Directory.CreateDirectory(_updatePath);
            Directory.CreateDirectory(_currentPath);
            Directory.CreateDirectory(_historyPath);
        }

        /// <summary>
        /// 從 Current 或 History 取得指定金鑰組的 RSA 金鑰，並快取結果。
        /// </summary>
        public RsaKeyModel GetRsaKey(string unifiedName)
        {
            lock (_keySetGate)
            {
                var name = PathSecurityHelper.ValidateUnifiedName(unifiedName);
                return _rsaCache.GetOrAdd(name, n => new Lazy<RsaKeyModel>(() => LoadRsaKeyInternal(n))).Value;
            }
        }

        /// <summary>
        /// 從 Current 或 History 取得指定金鑰組的 AES 金鑰，並快取結果。
        /// </summary>
        public SymmetricKeyModel GetAesKey(string unifiedName)
        {
            lock (_keySetGate)
            {
                var name = PathSecurityHelper.ValidateUnifiedName(unifiedName);
                return _aesCache.GetOrAdd(name, n => new Lazy<SymmetricKeyModel>(() => LoadAesKeyInternal(n))).Value;
            }
        }

        /// <summary>
        /// 執行一次 Update 資料夾的金鑰更新作業。
        /// 此方法保持完成更新後才回傳的既有語意。
        /// </summary>
        public Task StartAsync()
        {
            lock (_keySetGate)
            {
                Trace.TraceInformation("開始執行一次性金鑰更新處理。");
                ProcessUpdateFolder();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 取得 Current 資料夾中最新啟用的統一金鑰名稱。
        /// </summary>
        public string GetLatestActiveUnifiedName()
        {
            lock (_keySetGate)
            {
                return KeySetLayouts
                    .SelectMany(layout => Directory.GetFiles(_currentPath, "*" + layout.AesExtension)
                        .Select(path => Path.GetFileName(path).Substring(0, Path.GetFileName(path).Length - layout.AesExtension.Length))
                        .Where(name => HasKeySet(_currentPath, name, layout)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(name => name)
                    .FirstOrDefault() ?? throw new InvalidOperationException("Current 中沒有可用金鑰組。");
            }
        }

        private RsaKeyModel LoadRsaKeyInternal(string name)
        {
            var info = FindKeySet(name);

            return new RsaKeyModel { PublicKey = File.ReadAllText(info.RsaPublicKeyPath, Encoding.UTF8), PrivateKey = File.ReadAllText(info.RsaPrivateKeyPath, Encoding.UTF8) };
        }

        private SymmetricKeyModel LoadAesKeyInternal(string name)
        {
            var info = FindKeySet(name);
            var encrypted = File.ReadAllBytes(info.AesPath);
            var rsa = GetRsaKey(name);
            var material = info.UsesLegacyMaterial
                ? DecryptLegacyMaterial(encrypted, rsa)
                : Encoding.UTF8.GetString(_cryptoService.Decrypt(encrypted, CryptoAlgorithmType.RSA, rsa));
            var parts = info.UsesLegacyMaterial
                ? SplitLegacyMaterial(material)
                : material.Split(':');

            if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("AES 金鑰格式不正確");

            return new SymmetricKeyModel
            {
                Key = Convert.FromBase64String(parts[0]),
                IV = Convert.FromBase64String(parts[1])
            };
        }

        private KeySetInfo FindKeySet(string name)
        {
            foreach (var root in new[] { _currentPath, _historyPath })
            {
                foreach (var layout in KeySetLayouts)
                {
                    if (!HasKeySet(root, name, layout))
                        continue;

                    return new KeySetInfo
                    {
                        UnifiedName = name,
                        AesPath = Path.Combine(root, name + layout.AesExtension),
                        RsaPublicKeyPath = Path.Combine(root, name + layout.PublicKeyExtension),
                        RsaPrivateKeyPath = Path.Combine(root, name + layout.PrivateKeyExtension),
                        UsesLegacyMaterial = layout.UsesLegacyMaterial
                    };
                }
            }

            throw new InvalidOperationException($"找不到 {name} 的金鑰組。");
        }

        private void ProcessUpdateFolder()
        {
            Trace.TraceInformation("掃描金鑰更新目錄：{0}", _updatePath);

            var keySets = Directory.GetFiles(_updatePath)
                .Select(TryParseKeySetFile)
                .OfType<KeySetFile>()
                .GroupBy(file => file.Layout.Name + ":" + file.UnifiedName, StringComparer.OrdinalIgnoreCase);

            var updated = false;

            foreach (var keySet in keySets)
            {
                var first = keySet.First();
                var layout = first.Layout;
                var files = keySet.ToDictionary(file => file.Extension, file => file.Path, StringComparer.OrdinalIgnoreCase);

                if (layout.Extensions.Any(extension => !files.ContainsKey(extension)))
                {
                    Trace.TraceWarning("略過不完整的金鑰組：{0}", first.UnifiedName);
                    continue;
                }

                try
                {
                    // Publish the AES material last because it is used to discover active key sets.
                    CopyFileAtomically(files[layout.PublicKeyExtension], Path.Combine(_currentPath, first.UnifiedName + layout.PublicKeyExtension));
                    CopyFileAtomically(files[layout.PrivateKeyExtension], Path.Combine(_currentPath, first.UnifiedName + layout.PrivateKeyExtension));
                    CopyFileAtomically(files[layout.AesExtension], Path.Combine(_currentPath, first.UnifiedName + layout.AesExtension));

                    foreach (var file in files.Values)
                        File.Delete(file);

                    updated = true;
                }
                catch (Exception ex)
                {
                    // Keep source files for a retry. The in-process gate prevents consumers of
                    // this service from observing an in-progress update.
                    Trace.TraceError("更新金鑰組 {0} 失敗：{1}", first.UnifiedName, ex.Message);
                }
            }

            if (updated)
            {
                _rsaCache.Clear();
                _aesCache.Clear();
            }
        }

        private string DecryptLegacyMaterial(byte[] encrypted, RsaKeyModel rsa)
        {
            Exception? legacyException = null;

            try
            {
                var legacyMaterial = Encoding.UTF8.GetString(LegacyKeySetCrypto.Decrypt(encrypted, rsa));

                if (IsEncodedMaterial(legacyMaterial, '.'))
                    return legacyMaterial;
            }
            catch (Exception ex)
            {
                legacyException = ex;
            }

            // 保留先前版本短暫產生的 .der/OAEP/冒號資料可讀性；真正舊版檔案一律在上方
            // 以 PKCS#1 v1.5 與句點格式解析。部分 RSA implementations 可能對不匹配的
            // PKCS#1 block 回傳不可解析的 bytes 而不是拋例外，因此必須驗證材料再 fallback。
            try
            {
                var transitionalMaterial = Encoding.UTF8.GetString(_cryptoService.Decrypt(encrypted, CryptoAlgorithmType.RSA, rsa));

                if (IsEncodedMaterial(transitionalMaterial, ':'))
                    return transitionalMaterial;

                throw new InvalidDataException("AES 金鑰格式不正確");
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("無法解密舊版 AES 金鑰內容。", legacyException ?? ex);
            }
        }

        private static string[] SplitLegacyMaterial(string material)
        {
            var legacyParts = material.Split('.');

            if (legacyParts.Length == 2)
                return legacyParts;

            // 與 DecryptLegacyMaterial 的 OAEP fallback 對應，保留此前已產生的暫存 .der 檔。
            return material.Split(':');
        }

        private static bool IsEncodedMaterial(string material, char separator)
        {
            var parts = material.Split(separator);

            if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
                return false;

            try
            {
                Convert.FromBase64String(parts[0]);
                Convert.FromBase64String(parts[1]);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool HasKeySet(string root, string unifiedName, KeySetLayout layout)
        {
            return File.Exists(Path.Combine(root, unifiedName + layout.AesExtension))
                && File.Exists(Path.Combine(root, unifiedName + layout.PublicKeyExtension))
                && File.Exists(Path.Combine(root, unifiedName + layout.PrivateKeyExtension));
        }

        private static KeySetFile? TryParseKeySetFile(string path)
        {
            var fileName = Path.GetFileName(path);

            foreach (var layout in KeySetLayouts)
            {
                foreach (var extension in layout.Extensions)
                {
                    if (!fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) || fileName.Length == extension.Length)
                        continue;

                    var unifiedName = fileName.Substring(0, fileName.Length - extension.Length);

                    if (PathSecurityHelper.IsSafeUnifiedName(unifiedName))
                        return new KeySetFile(path, unifiedName, extension, layout);
                }
            }

            return null;
        }

        private static void CopyFileAtomically(string sourcePath, string targetPath)
        {
            var temporaryPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                File.Copy(sourcePath, temporaryPath, true);

                if (File.Exists(targetPath))
                    File.Replace(temporaryPath, targetPath, null);
                else
                    File.Move(temporaryPath, targetPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private sealed class KeySetLayout
        {
            public KeySetLayout(string name, string aesExtension, string publicKeyExtension, string privateKeyExtension, bool usesLegacyMaterial)
            {
                Name = name;
                AesExtension = aesExtension;
                PublicKeyExtension = publicKeyExtension;
                PrivateKeyExtension = privateKeyExtension;
                UsesLegacyMaterial = usesLegacyMaterial;
                Extensions = new[] { aesExtension, publicKeyExtension, privateKeyExtension };
            }

            public string Name { get; }
            public string AesExtension { get; }
            public string PublicKeyExtension { get; }
            public string PrivateKeyExtension { get; }
            public bool UsesLegacyMaterial { get; }
            public string[] Extensions { get; }
        }

        private sealed class KeySetFile
        {
            public KeySetFile(string path, string unifiedName, string extension, KeySetLayout layout)
            {
                Path = path;
                UnifiedName = unifiedName;
                Extension = extension;
                Layout = layout;
            }

            public string Path { get; }
            public string UnifiedName { get; }
            public string Extension { get; }
            public KeySetLayout Layout { get; }
        }
    }
}
