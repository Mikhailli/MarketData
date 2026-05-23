using FluentAssertions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Normalizers;

namespace MarketData.Tests.Normalizers;

public sealed class BinanceMessageNormalizerTests
{
    [Fact]
    public void TryNormalize_ShouldParseTradeMessage()
    {
        var normalizer = new BinanceMessageNormalizer();
        var raw = new RawExchangeMessage
        {
            Exchange = "Binance",
            Payload =
                """
                {
                  "e": "trade",
                  "s": "BTCUSDT",
                  "p": "65000.12000000",
                  "q": "0.01000000",
                  "T": 1710000000123
                }
                """
        };

        var result = normalizer.TryNormalize(raw, out var ticks);

        result.Should().BeTrue();

        var tick = ticks.Should().ContainSingle().Subject;
        tick.Exchange.Should().Be("Binance");
        tick.Symbol.Should().Be("BTCUSDT");
        tick.Price.Should().Be(65000.12000000m);
        tick.Volume.Should().Be(0.01000000m);
        tick.TimestampUtc.Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1710000000123).UtcDateTime);
    }

    [Fact]
    public void TryNormalize_ShouldIgnoreNonTradeMessage()
    {
        var normalizer = new BinanceMessageNormalizer();
        var raw = new RawExchangeMessage
        {
            Exchange = "Binance",
            Payload = """{"result": null, "id": 1}"""
        };

        var result = normalizer.TryNormalize(raw, out var ticks);

        result.Should().BeFalse();
        ticks.Should().BeEmpty();
    }
}
