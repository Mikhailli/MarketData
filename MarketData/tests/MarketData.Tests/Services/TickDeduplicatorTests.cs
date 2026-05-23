using FluentAssertions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Options;
using MarketData.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace MarketData.Tests.Services;

public sealed class TickDeduplicatorTests
{
    [Fact]
    public void IsDuplicate_ShouldDetectRepeatedTick()
    {
        var deduplicator = new TickDeduplicator(
            Options.Create(new DeduplicationOptions()),
            TimeProvider.System);

        var tick = new Tick
        {
            Exchange = "Binance",
            Symbol = "BTCUSDT",
            Price = 65000m,
            Volume = 0.1m,
            TimestampUtc = DateTime.UtcNow
        };

        deduplicator.IsDuplicate(tick).Should().BeFalse();
        deduplicator.IsDuplicate(tick).Should().BeTrue();
    }

    [Fact]
    public void IsDuplicate_ShouldTreatDifferentVolumeAsDifferentTick()
    {
        var deduplicator = new TickDeduplicator(
            Options.Create(new DeduplicationOptions()),
            TimeProvider.System);

        var timestamp = DateTime.UtcNow;
        var first = new Tick
        {
            Exchange = "Binance",
            Symbol = "BTCUSDT",
            Price = 65000m,
            Volume = 0.1m,
            TimestampUtc = timestamp
        };

        var second = first with { Volume = 0.2m };

        deduplicator.IsDuplicate(first).Should().BeFalse();
        deduplicator.IsDuplicate(second).Should().BeFalse();
    }
}
