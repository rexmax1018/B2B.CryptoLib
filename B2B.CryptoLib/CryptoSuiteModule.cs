using Autofac;
using B2B.CryptoLib.Factories;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.Services;

namespace B2B.CryptoLib
{
    /// <summary>
    /// 將 CryptoSuite 服務及其相依性註冊到 Autofac 容器。
    /// </summary>
    public class CryptoSuiteModule : Module
    {
        private readonly string _basePath;

        public CryptoSuiteModule(string keyManagerBasePath)
        {
            _basePath = keyManagerBasePath;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CryptoKeyService>().As<ICryptoKeyService>().SingleInstance();

            builder.RegisterType<CryptoService>().As<ICryptoService>().SingleInstance();

            builder.RegisterType<KeyLoaderFactory>().As<IKeyLoaderFactory>().SingleInstance();

            builder.RegisterType<KeyManagerService>().AsSelf().WithParameter("basePath", _basePath).SingleInstance();

            builder.RegisterType<DataEncryptionService>().As<IDataEncryptionService>().SingleInstance();
        }
    }
}
