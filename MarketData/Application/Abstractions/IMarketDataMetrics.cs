namespace MarketData.Application.Abstractions;

public interface IMarketDataMetrics
{
    void IncrementRawMessagesReceived(string exchange);

    void AddTicksProcessed(long count);

    void AddTicksPersisted(long count);

    void IncrementDuplicatesSkipped();

    void IncrementNormalizationFailures();

    void IncrementPersistenceFailures();

    void IncrementReconnects(string exchange);

    MarketDataMetricsSnapshot Snapshot();
}
