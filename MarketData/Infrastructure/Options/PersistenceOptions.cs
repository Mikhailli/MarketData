namespace MarketData.Infrastructure.Options;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public int BatchSize { get; set; } = 250;

    public int FlushIntervalMilliseconds { get; set; } = 500;

    public int FailedFlushDelayMilliseconds { get; set; } = 1_000;
}
