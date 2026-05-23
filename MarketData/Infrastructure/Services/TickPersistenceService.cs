using System.Threading.Channels;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MarketData.Infrastructure.Services;

public sealed class TickPersistenceService : BackgroundService
{
    private readonly ChannelReader<Tick> _reader;
    private readonly ITickRepository _repository;
    private readonly IMarketDataMetrics _metrics;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TickPersistenceService> _logger;
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private readonly TimeSpan _failedFlushDelay;

    public TickPersistenceService(
        Channel<Tick> tickChannel,
        ITickRepository repository,
        IMarketDataMetrics metrics,
        TimeProvider timeProvider,
        IOptions<PersistenceOptions> options,
        ILogger<TickPersistenceService> logger)
    {
        _reader = tickChannel.Reader;
        _repository = repository;
        _metrics = metrics;
        _timeProvider = timeProvider;
        _logger = logger;

        var currentOptions = options.Value;
        _batchSize = Math.Max(1, currentOptions.BatchSize);
        _flushInterval = TimeSpan.FromMilliseconds(Math.Max(50, currentOptions.FlushIntervalMilliseconds));
        _failedFlushDelay = TimeSpan.FromMilliseconds(Math.Max(100, currentOptions.FailedFlushDelayMilliseconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new List<Tick>(_batchSize);
        var lastFlush = _timeProvider.GetUtcNow();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var tick = await _reader.ReadAsync(stoppingToken).AsTask()
                        .WaitAsync(_flushInterval, stoppingToken);

                    buffer.Add(tick);
                    DrainAvailableTicks(buffer);
                }
                catch (TimeoutException)
                {
                    // Time-based flush below.
                }

                var flushBySize = buffer.Count >= _batchSize;
                var flushByTime = buffer.Count > 0
                                  && _timeProvider.GetUtcNow() - lastFlush >= _flushInterval;

                if (flushBySize || flushByTime)
                {
                    var flushed = await FlushAsync(buffer, stoppingToken);

                    if (flushed)
                    {
                        lastFlush = _timeProvider.GetUtcNow();
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Persistence worker is stopping");
        }
        finally
        {
            DrainAvailableTicks(buffer);

            if (buffer.Count > 0)
            {
                using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await FlushAsync(buffer, shutdownCts.Token);
            }
        }
    }

    private void DrainAvailableTicks(List<Tick> buffer)
    {
        while (buffer.Count < _batchSize && _reader.TryRead(out var nextTick))
        {
            buffer.Add(nextTick);
        }
    }

    private async Task<bool> FlushAsync(
        List<Tick> buffer,
        CancellationToken cancellationToken)
    {
        if (buffer.Count == 0)
        {
            return true;
        }

        var batch = buffer.ToArray();

        try
        {
            var saved = await _repository.SaveBatchAsync(batch, cancellationToken);
            _metrics.AddTicksPersisted(saved);

            _logger.LogInformation(
                "Persisted {Saved}/{Count} ticks",
                saved,
                batch.Length);

            buffer.Clear();
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _metrics.IncrementPersistenceFailures();
            _logger.LogError(ex, "Failed to persist {Count} ticks", batch.Length);

            await Task.Delay(_failedFlushDelay, cancellationToken);
            return false;
        }
    }
}
