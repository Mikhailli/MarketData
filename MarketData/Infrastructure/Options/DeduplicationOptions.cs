namespace MarketData.Infrastructure.Options;

public sealed class DeduplicationOptions
{
    public const string SectionName = "Deduplication";

    public int TtlSeconds { get; set; } = 60;

    public int CleanupIntervalSeconds { get; set; } = 5;
}
