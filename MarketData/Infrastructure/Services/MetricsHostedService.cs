using MarketData.Application.Abstractions;

namespace MarketData.Infrastructure.Services;

public sealed class MetricsHostedService(
    IMarketDataMetrics metrics,
    TimeProvider timeProvider,
    ILogger<MetricsHostedService> logger) : BackgroundService
{
    private readonly IMarketDataMetrics _metrics = metrics;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<MetricsHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var previousSnapshot = _metrics.Snapshot();
        var previousTimestamp = _timeProvider.GetUtcNow();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            var currentTimestamp = _timeProvider.GetUtcNow();
            var currentSnapshot = _metrics.Snapshot();
            var elapsedSeconds = Math.Max(1, (currentTimestamp - previousTimestamp).TotalSeconds);

            var processedPerSecond = (currentSnapshot.TicksProcessed - previousSnapshot.TicksProcessed)
                                     / elapsedSeconds;
            var persistedPerSecond = (currentSnapshot.TicksPersisted - previousSnapshot.TicksPersisted)
                                     / elapsedSeconds;

            _logger.LogInformation(
                "Metrics: raw={Raw}, processed={Processed} ({ProcessedPerSecond:F1}/sec), persisted={Persisted} ({PersistedPerSecond:F1}/sec), duplicates={Duplicates}, parseFailures={ParseFailures}, dbFailures={DbFailures}, reconnects={Reconnects}",
                currentSnapshot.RawMessagesReceived,
                currentSnapshot.TicksProcessed,
                processedPerSecond,
                currentSnapshot.TicksPersisted,
                persistedPerSecond,
                currentSnapshot.DuplicatesSkipped,
                currentSnapshot.NormalizationFailures,
                currentSnapshot.PersistenceFailures,
                currentSnapshot.Reconnects);

            previousSnapshot = currentSnapshot;
            previousTimestamp = currentTimestamp;
        }
    }
}
