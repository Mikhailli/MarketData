using MarketData.Application.Abstractions;

namespace MarketData.Infrastructure.Normalizers;

public sealed class MessageNormalizerFactory : IMessageNormalizerFactory
{
    private readonly IReadOnlyDictionary<string, IMessageNormalizer> _normalizers;

    public MessageNormalizerFactory(IEnumerable<IMessageNormalizer> normalizers)
    {
        var map = new Dictionary<string, IMessageNormalizer>(StringComparer.OrdinalIgnoreCase);

        foreach (var normalizer in normalizers)
        {
            if (!map.TryAdd(normalizer.Exchange, normalizer))
            {
                throw new InvalidOperationException(
                    $"Duplicate normalizer registration for exchange '{normalizer.Exchange}'.");
            }
        }

        _normalizers = map;
    }

    public IMessageNormalizer? GetNormalizer(string exchange)
    {
        return _normalizers.GetValueOrDefault(exchange);
    }
}
