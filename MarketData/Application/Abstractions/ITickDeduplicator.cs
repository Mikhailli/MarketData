using MarketData.Domain.Entities;

namespace MarketData.Application.Abstractions;

public interface ITickDeduplicator
{
    bool IsDuplicate(Tick tick);
}
