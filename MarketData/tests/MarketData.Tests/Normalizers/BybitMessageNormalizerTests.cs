using FluentAssertions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Normalizers;

namespace MarketData.Tests.Normalizers;

public sealed class BybitMessageNormalizerTests
{
    [Fact]
    public void TryNormalize_ShouldParseTradeBatch()
    {
        var normalizer = new BybitMessageNormalizer();
        var raw = new RawExchangeMessage
        {
            Exchange = "Bybit",
            Payload =
                """
                {
                  "topic": "publicTrade.BTCUSDT",
                  "type": "snapshot",
                  "data": [
                    { "T": 1710000000001, "s": "BTCUSDT", "p": "65001.1", "v": "0.1" },
                    { "T": 1710000000002, "s": "BTCUSDT", "p": "65001.2", "v": "0.2" }
                  ]
                }
                """
        };

        var result = normalizer.TryNormalize(raw, out var ticks);

        result.Should().BeTrue();
        ticks.Should().HaveCount(2);
        ticks.Select(t => t.Price).Should().Equal(65001.1m, 65001.2m);
    }
}
