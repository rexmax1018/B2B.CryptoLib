using Autofac;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.KeyGeneration.Factories;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Services;
using B2B.CryptoLib.Services;

namespace B2B.CryptoLib.KeyGeneration
{
    /// <summary>
    /// 僅供離線 KeyGenTool 使用的 Autofac 註冊；WebAPI 不得參考或註冊此 module。
    /// </summary>
    public class KeyGenerationModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<KeyGeneratorFactory>().As<IKeyGeneratorFactory>().SingleInstance();

            builder.RegisterType<KeyGenerationService>().As<IKeyGenerationService>().SingleInstance();

            builder.RegisterType<CryptoService>().As<ICryptoService>().SingleInstance();

            builder.RegisterType<KeySetGenerationService>().As<IKeySetGenerationService>().SingleInstance();
        }
    }
}
