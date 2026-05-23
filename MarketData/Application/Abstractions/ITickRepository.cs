using MarketData.Domain.Entities;

namespace MarketData.Application.Abstractions;

public interface ITickRepository
{
    Task<int> SaveBatchAsync(
        IReadOnlyCollection<Tick> ticks,
        CancellationToken cancellationToken);
}
