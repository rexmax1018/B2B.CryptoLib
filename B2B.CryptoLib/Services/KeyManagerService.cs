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
    /// </summary>
    public class KeyManagerService
    {
        private readonly string _basePath;
        private readonly string _updatePath;
        private readonly string _currentPath;
        private readonly string _historyPath;
        private readonly ICryptoService _cryptoService;

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
        public RsaKeyModel GetRsaKey(string unifiedName) => _rsaCache.GetOrAdd(PathSecurityHelper.ValidateUnifiedName(unifiedName), n => new Lazy<RsaKeyModel>(() => LoadRsaKeyInternal(n))).Value;

        /// <summary>
        /// 從 Current 或 History 取得指定金鑰組的 AES 金鑰，並快取結果。
        /// </summary>
        public SymmetricKeyModel GetAesKey(string unifiedName) => _aesCache.GetOrAdd(PathSecurityHelper.ValidateUnifiedName(unifiedName), n => new Lazy<SymmetricKeyModel>(() => LoadAesKeyInternal(n))).Value;

        /// <summary>
        /// 執行一次 Update 資料夾的金鑰更新作業。
        /// </summary>
        public Task StartAsync()
        {
            Trace.TraceInformation("開始執行一次性金鑰更新處理。");

            ProcessUpdateFolder();

            return Task.CompletedTask;
        }

        /// <summary>
        /// 取得 Current 資料夾中最新啟用的統一金鑰名稱。
        /// </summary>
        public string GetLatestActiveUnifiedName() => Directory.GetFiles(_currentPath, "*.aes").Select(Path.GetFileNameWithoutExtension).OrderByDescending(x => x).FirstOrDefault() ?? throw new InvalidOperationException("Current 中沒有可用金鑰組。");

        private RsaKeyModel LoadRsaKeyInternal(string name)
        {
            var info = FindKeySet(name);

            return new RsaKeyModel { PublicKey = File.ReadAllText(info.RsaPublicKeyPath, Encoding.UTF8), PrivateKey = File.ReadAllText(info.RsaPrivateKeyPath, Encoding.UTF8) };
        }

        private SymmetricKeyModel LoadAesKeyInternal(string name)
        {
            var info = FindKeySet(name);
            var encrypted = File.ReadAllBytes(info.AesPath);
            var plain = _cryptoService.Decrypt(encrypted, CryptoAlgorithmType.RSA, GetRsaKey(name));
            var parts = Encoding.UTF8.GetString(plain).Split(':');

            if (parts.Length != 2)
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
                var aes = Path.Combine(root, name + ".aes");
                var pub = Path.Combine(root, name + ".pub");
                var priv = Path.Combine(root, name + ".priv");

                if (File.Exists(aes) && File.Exists(pub) && File.Exists(priv))
                    return new KeySetInfo
                    {
                        UnifiedName = name,
                        AesPath = aes,
                        RsaPublicKeyPath = pub,
                        RsaPrivateKeyPath = priv
                    };
            }

            throw new InvalidOperationException($"找不到 {name} 的金鑰組。");
        }

        private void ProcessUpdateFolder()
        {
            Trace.TraceInformation("掃描金鑰更新目錄：{0}", _updatePath);

            foreach (var file in Directory.GetFiles(_updatePath))
            {
                var target = Path.Combine(_currentPath, Path.GetFileName(file));

                File.Copy(file, target, true);
                File.Delete(file);
            }

            _rsaCache.Clear();
            _aesCache.Clear();
        }
    }
}
