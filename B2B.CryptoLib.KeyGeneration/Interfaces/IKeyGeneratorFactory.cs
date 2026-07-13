using B2B.CryptoLib.Enums;

namespace B2B.CryptoLib.KeyGeneration.Interfaces
{
    public interface IKeyGeneratorFactory
    {
        IKeyGenerator<TModel> Create<TModel>(CryptoAlgorithmType algorithm) where TModel : class;
    }
}
