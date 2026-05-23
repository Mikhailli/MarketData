using FluentAssertions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Normalizers;

namespace MarketData.Tests.Normalizers;

public sealed class KrakenMessageNormalizerTests
{
    [Fact]
    public void TryNormalize_ShouldParseTradeMessage()
    {
        var normalizer = new KrakenMessageNormalizer();
        var raw = new RawExchangeMessage
        {
            Exchange = "Kraken",
            Payload =
                """
                {
                  "channel": "trade",
                  "data": [
                    {
                      "symbol": "BTC/USD",
                      "price": 65002.5,
                      "qty": 0.3,
                      "timestamp": "2024-03-09T16:00:00.123Z"
                    }
                  ]
                }
                """
        };

        var result = normalizer.TryNormalize(raw, out var ticks);

        result.Should().BeTrue();

        var tick = ticks.Should().ContainSingle().Subject;
        tick.Symbol.Should().Be("BTCUSD");
        tick.Price.Should().Be(65002.5m);
        tick.Volume.Should().Be(0.3m);
        tick.TimestampUtc.Should().Be(DateTime.Parse(
            "2024-03-09T16:00:00.123Z",
            null,
            System.Globalization.DateTimeStyles.AdjustToUniversal));
    }
}
