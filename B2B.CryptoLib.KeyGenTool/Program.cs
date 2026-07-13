using System;
using Autofac;
using B2B.CryptoLib.Config;
using B2B.CryptoLib.Enums;
using B2B.CryptoLib.KeyGeneration;
using B2B.CryptoLib.KeyGeneration.Interfaces;
using B2B.CryptoLib.Models;

namespace B2B.CryptoLib.KeyGenTool
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.Error.WriteLine("Usage: B2B.CryptoLib.KeyGenTool.exe <AES|RSA|ECC> [fileName]");

                return 1;
            }

            try
            {
                CryptoConfig.Load();

                var builder = new ContainerBuilder();

                builder.RegisterModule(new KeyGenerationModule());

                using (var container = builder.Build())
                {
                    var service = container.Resolve<IKeyGenerationService>();
                    var result = Generate(service, args[0], args.Length == 2 ? args[1] : null);

                    Console.WriteLine(result);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                return 2;
            }
        }

        private static object Generate(IKeyGenerationService service, string algorithm, string fileName)
        {
            switch (algorithm.ToUpperInvariant())
            {
                case "AES":
                    return service.GenerateAndSaveKey<SymmetricKeyModel>(CryptoAlgorithmType.AES, fileName);

                case "RSA":
                    return service.GenerateAndSaveKey<RsaKeyModel>(CryptoAlgorithmType.RSA, fileName);

                case "ECC":
                    return service.GenerateAndSaveKey<EccKeyModel>(CryptoAlgorithmType.ECC, fileName);

                default:
                    throw new ArgumentException("演算法必須為 AES、RSA 或 ECC。", nameof(algorithm));
            }
        }
    }
}