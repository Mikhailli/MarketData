namespace MarketData.Application.Abstractions;

public interface IMessageNormalizerFactory
{
    IMessageNormalizer? GetNormalizer(string exchange);
}
