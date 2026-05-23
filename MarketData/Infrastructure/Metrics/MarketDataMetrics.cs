using MarketData.Application.Abstractions;

namespace MarketData.Infrastructure.Metrics;

public sealed class MarketDataMetrics : IMarketDataMetrics
{
    private long _rawMessagesReceived;
    private long _ticksProcessed;
    private long _ticksPersisted;
    private long _duplicatesSkipped;
    private long _normalizationFailures;
    private long _persistenceFailures;
    private long _reconnects;

    public void IncrementRawMessagesReceived(string exchange)
    {
        Interlocked.Increment(ref _rawMessagesReceived);
    }

    public void AddTicksProcessed(long count)
    {
        Interlocked.Add(ref _ticksProcessed, count);
    }

    public void AddTicksPersisted(long count)
    {
        Interlocked.Add(ref _ticksPersisted, count);
    }

    public void IncrementDuplicatesSkipped()
    {
        Interlocked.Increment(ref _duplicatesSkipped);
    }

    public void IncrementNormalizationFailures()
    {
        Interlocked.Increment(ref _normalizationFailures);
    }

    public void IncrementPersistenceFailures()
    {
        Interlocked.Increment(ref _persistenceFailures);
    }

    public void IncrementReconnects(string exchange)
    {
        Interlocked.Increment(ref _reconnects);
    }

    public MarketDataMetricsSnapshot Snapshot()
    {
        return new MarketDataMetricsSnapshot(
            Interlocked.Read(ref _rawMessagesReceived),
            Interlocked.Read(ref _ticksProcessed),
            Interlocked.Read(ref _ticksPersisted),
            Interlocked.Read(ref _duplicatesSkipped),
            Interlocked.Read(ref _normalizationFailures),
            Interlocked.Read(ref _persistenceFailures),
            Interlocked.Read(ref _reconnects));
    }
}
