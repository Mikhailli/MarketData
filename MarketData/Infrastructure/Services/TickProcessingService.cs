using System.Threading.Channels;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MarketData.Infrastructure.Services;

public sealed class TickProcessingService(
    Channel<RawExchangeMessage> rawChannel,
    Channel<Tick> tickChannel,
    IMessageNormalizerFactory normalizerFactory,
    ITickDeduplicator deduplicator,
    IMarketDataMetrics metrics,
    IOptions<PipelineOptions> options,
    ILogger<TickProcessingService> logger) : BackgroundService
{
    private readonly ChannelReader<RawExchangeMessage> _rawReader = rawChannel.Reader;
    private readonly ChannelWriter<Tick> _tickWriter = tickChannel.Writer;
    private readonly IMessageNormalizerFactory _normalizerFactory = normalizerFactory;
    private readonly ITickDeduplicator _deduplicator = deduplicator;
    private readonly IMarketDataMetrics _metrics = metrics;
    private readonly ILogger<TickProcessingService> _logger = logger;
    private readonly int _workerCount = Math.Max(1, options.Value.NormalizerWorkerCount);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting {WorkerCount} normalizer workers", _workerCount);

        var workers = Enumerable.Range(1, _workerCount)
            .Select(workerId => ProcessAsync(workerId, stoppingToken))
            .ToArray();

        await Task.WhenAll(workers);
    }

    private async Task ProcessAsync(
        int workerId,
        CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var raw in _rawReader.ReadAllAsync(stoppingToken))
            {
                await ProcessMessageAsync(raw, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Normalizer worker {WorkerId} stopped", workerId);
        }
    }

    private async Task ProcessMessageAsync(
        RawExchangeMessage raw,
        CancellationToken stoppingToken)
    {
        var normalizer = _normalizerFactory.GetNormalizer(raw.Exchange);

        if (normalizer is null)
        {
            _metrics.IncrementNormalizationFailures();
            _logger.LogWarning("No normalizer registered for exchange {Exchange}", raw.Exchange);
            return;
        }

        if (!normalizer.TryNormalize(raw, out var ticks))
        {
            _metrics.IncrementNormalizationFailures();
            return;
        }

        var processed = 0;

        foreach (var tick in ticks)
        {
            if (_deduplicator.IsDuplicate(tick))
            {
                _metrics.IncrementDuplicatesSkipped();
                continue;
            }

            await _tickWriter.WriteAsync(tick, stoppingToken);
            processed++;
        }

        if (processed > 0)
        {
            _metrics.AddTicksProcessed(processed);
        }
    }
}
