using MarketData.Domain.Entities;

namespace MarketData.Application.Abstractions;

public interface IMessageNormalizer
{
    string Exchange { get; }

    bool TryNormalize(
        RawExchangeMessage raw,
        out IReadOnlyCollection<Tick> ticks);
}
