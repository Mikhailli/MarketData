namespace MarketData.Application.Abstractions;

public sealed record MarketDataMetricsSnapshot(
    long RawMessagesReceived,
    long TicksProcessed,
    long TicksPersisted,
    long DuplicatesSkipped,
    long NormalizationFailures,
    long PersistenceFailures,
    long Reconnects);
