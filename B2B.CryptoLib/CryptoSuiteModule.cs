using Autofac;
using B2B.CryptoLib.Factories;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Services;

namespace B2B.CryptoLib
{
    /// <summary>
    /// 將 CryptoSuite 服務及其相依性註冊到 Autofac 容器。
    /// </summary>
    /// <remarks>
    /// 這是可選的 DI 整合層；不使用 Autofac 的宿主可直接使用
    /// <see cref="CryptoClient"/>。模組註冊 runtime 的金鑰載入、加密與管理服務，
    /// 不會註冊離線金鑰產生服務。所有註冊均為 singleton，因此同一個 container
    /// 內的 client 共享同一個 <see cref="KeyManagerService"/> 與其 instance-local
    /// cache。
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = new ContainerBuilder();
    /// builder.RegisterModule(new CryptoSuiteModule(@"C:\CryptoKeys", "tenant-a"));
    /// using var container = builder.Build();
    /// var client = container.Resolve&lt;ICryptoClient&gt;();
    /// </code>
    /// </example>
    public class CryptoSuiteModule : Module
    {
        private readonly string _basePath;
        private readonly string? _activeUnifiedName;

        /// <summary>
        /// 建立使用指定金鑰管理根目錄、但不預設 active unified name 的 module。
        /// </summary>
        /// <param name="keyManagerBasePath">包含 <c>current</c>、<c>history</c> 與 <c>update</c> 子目錄的根目錄。</param>
        /// <remarks>
        /// 使用此建構函式註冊的 <see cref="ICryptoClient"/> 必須呼叫帶 unified name
        /// 的 <see cref="ICryptoClient.Encrypt(string?, string?)"/> overload。
        /// </remarks>
        public CryptoSuiteModule(string keyManagerBasePath)
            : this(keyManagerBasePath, null)
        {
        }

        /// <summary>
        /// 建立使用指定金鑰根目錄與可選 active unified name 的 module。
        /// </summary>
        /// <param name="keyManagerBasePath">包含 <c>current</c>、<c>history</c> 與 <c>update</c> 子目錄的根目錄。</param>
        /// <param name="activeUnifiedName">供無名稱加密 overload 使用的金鑰名稱；可為 <see langword="null"/>。</param>
        /// <remarks>
        /// active name 只影響由 module 建立的 <see cref="ICryptoClient"/>；其他低階
        /// service 仍要求呼叫端明確提供金鑰或 unified name。module 不會在建構或
        /// resolve 時自動消費 <c>update</c> 檔案。
        /// </remarks>
        public CryptoSuiteModule(string keyManagerBasePath, string? activeUnifiedName)
        {
            _basePath = keyManagerBasePath;
            _activeUnifiedName = activeUnifiedName;
        }

        /// <summary>
        /// 將 CryptoSuite runtime 元件加入 Autofac builder。
        /// </summary>
        /// <param name="builder">要接收服務註冊的 Autofac builder。</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> 為 <see langword="null"/>。</exception>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CryptoKeyService>().As<ICryptoKeyService>().SingleInstance();

            builder.RegisterType<CryptoService>().As<ICryptoService>().SingleInstance();

            builder.RegisterType<KeyLoaderFactory>().As<IKeyLoaderFactory>().SingleInstance();

            builder.RegisterType<KeyManagerService>().AsSelf().WithParameter("basePath", _basePath).SingleInstance();

            builder.RegisterType<DataEncryptionService>().AsSelf().As<IDataEncryptionService>().SingleInstance();

            builder.Register(c => new CryptoClient(c.Resolve<IDataEncryptionService>(), c.Resolve<KeyManagerService>(), _activeUnifiedName))
                .AsSelf()
                .As<ICryptoClient>()
                .SingleInstance();
        }
    }
}
