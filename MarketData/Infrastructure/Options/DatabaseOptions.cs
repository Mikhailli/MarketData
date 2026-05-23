namespace MarketData.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionStringName { get; set; } = "Postgres";

    public string? ConnectionString { get; set; }

    public bool EnsureCreated { get; set; } = true;

    public int InitializationRetryCount { get; set; } = 10;

    public int InitializationRetryDelayMilliseconds { get; set; } = 1_000;
}
