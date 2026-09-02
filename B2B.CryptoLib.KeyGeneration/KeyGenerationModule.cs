using Autofac;
using B2B.CryptoLib.Interfaces;
using B2B.CryptoLib.KeyGeneration.Factories;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.KeyGeneration.Services;
using B2B.CryptoLib.Services;

namespace B2B.CryptoLib.KeyGeneration
{
    /// <summary>
    /// 僅供離線 KeyGenTool 使用的 Autofac 註冊；WebAPI 不得參考或註冊此模組。
    /// </summary>
    /// <remarks>
    /// 模組只註冊產生器、金鑰組產生服務與低階密碼服務；它不應被
    /// 部署到 WebAPI 執行階段，因為金鑰產生與保存是離線職責。產生器使用
    /// <see cref="B2B.CryptoLib.Config.CryptoConfig"/> 的舊版程序層級設定。
    /// </remarks>
    public class KeyGenerationModule : Module
    {
        /// <summary>將離線金鑰產生所需的服務加入 Autofac 建置器。</summary>
        /// <param name="builder">要接收註冊的 Autofac 建置器。</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<KeyGeneratorFactory>().As<IKeyGeneratorFactory>().SingleInstance();

            builder.RegisterType<KeyGenerationService>().As<IKeyGenerationService>().SingleInstance();

            builder.RegisterType<CryptoService>().As<ICryptoService>().SingleInstance();

            builder.RegisterType<KeySetGenerationService>().As<IKeySetGenerationService>().SingleInstance();
        }
    }
}
