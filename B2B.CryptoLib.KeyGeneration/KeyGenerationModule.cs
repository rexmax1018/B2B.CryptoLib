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
    /// <remarks>
    /// module 只註冊 generator、key-set generation 與低階 crypto service；它不應被
    /// 部署到 WebAPI runtime，因為金鑰產生與保存是離線職責。產生器使用
    /// <see cref="B2B.CryptoLib.Config.CryptoConfig"/> 的 legacy process-wide 設定。
    /// </remarks>
    public class KeyGenerationModule : Module
    {
        /// <summary>將離線金鑰產生所需的 services 加入 Autofac builder。</summary>
        /// <param name="builder">要接收註冊的 Autofac builder。</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<KeyGeneratorFactory>().As<IKeyGeneratorFactory>().SingleInstance();

            builder.RegisterType<KeyGenerationService>().As<IKeyGenerationService>().SingleInstance();

            builder.RegisterType<CryptoService>().As<ICryptoService>().SingleInstance();

            builder.RegisterType<KeySetGenerationService>().As<IKeySetGenerationService>().SingleInstance();
        }
    }
}
