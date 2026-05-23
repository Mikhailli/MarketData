using System.Collections.Concurrent;
using System.Globalization;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MarketData.Infrastructure.Services;

public sealed class TickDeduplicator : ITickDeduplicator
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cache = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _ttl;
    private readonly TimeSpan _cleanupInterval;

    private long _nextCleanupTicks;
    private int _cleanupInProgress;

    public TickDeduplicator(
        IOptions<DeduplicationOptions> options,
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        var currentOptions = options.Value;
        _ttl = TimeSpan.FromSeconds(Math.Max(1, currentOptions.TtlSeconds));
        _cleanupInterval = TimeSpan.FromSeconds(Math.Max(1, currentOptions.CleanupIntervalSeconds));
        _nextCleanupTicks = timeProvider.GetUtcNow().Add(_cleanupInterval).Ticks;
    }

    public bool IsDuplicate(Tick tick)
    {
        var now = _timeProvider.GetUtcNow();
        CleanupIfNeeded(now);

        var key = BuildKey(tick);
        return !_cache.TryAdd(key, now);
    }

    private void CleanupIfNeeded(DateTimeOffset now)
    {
        if (now.Ticks < Volatile.Read(ref _nextCleanupTicks))
        {
            return;
        }

        if (Interlocked.Exchange(ref _cleanupInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            foreach (var item in _cache)
            {
                if (now - item.Value > _ttl)
                {
                    _cache.TryRemove(item.Key, out _);
                }
            }

            Volatile.Write(ref _nextCleanupTicks, now.Add(_cleanupInterval).Ticks);
        }
        finally
        {
            Volatile.Write(ref _cleanupInProgress, 0);
        }
    }

    private static string BuildKey(Tick tick)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{tick.Exchange}:{tick.Symbol}:{tick.Price}:{tick.Volume}:{tick.TimestampUtc.Ticks}");
    }
}
