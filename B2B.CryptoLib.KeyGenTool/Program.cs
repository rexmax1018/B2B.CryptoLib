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
                Console.Error.WriteLine("Usage: B2B.CryptoLib.KeyGenTool.exe <AES|RSA|ECC|KEYSET> [fileName]");

                return 1;
            }

            try
            {
                CryptoConfig.Load();

                var builder = new ContainerBuilder();

                builder.RegisterModule(new KeyGenerationModule());

                using (var container = builder.Build())
                {
                    var result = Generate(container, args[0], args.Length == 2 ? args[1] : null);

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

        private static object Generate(IContainer container, string algorithm, string? fileName)
        {
            switch (algorithm.ToUpperInvariant())
            {
                case "AES":
                    return container.Resolve<IKeyGenerationService>().GenerateAndSaveKey<SymmetricKeyModel>(CryptoAlgorithmType.AES, fileName);

                case "RSA":
                    return container.Resolve<IKeyGenerationService>().GenerateAndSaveKey<RsaKeyModel>(CryptoAlgorithmType.RSA, fileName);

                case "ECC":
                    return container.Resolve<IKeyGenerationService>().GenerateAndSaveKey<EccKeyModel>(CryptoAlgorithmType.ECC, fileName);

                case "KEYSET":
                    return container.Resolve<IKeySetGenerationService>().GenerateAndSave(fileName);

                default:
                    throw new ArgumentException("演算法必須為 AES、RSA、ECC 或 KEYSET。", nameof(algorithm));
            }
        }
    }
}
