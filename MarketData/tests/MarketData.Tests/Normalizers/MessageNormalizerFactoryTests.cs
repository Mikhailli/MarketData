using FluentAssertions;
using MarketData.Application.Abstractions;
using MarketData.Infrastructure.Normalizers;

namespace MarketData.Tests.Normalizers;

public sealed class MessageNormalizerFactoryTests
{
    [Fact]
    public void GetNormalizer_ShouldResolveExchangeCaseInsensitively()
    {
        IMessageNormalizer[] normalizers =
        [
            new BinanceMessageNormalizer()
        ];

        var factory = new MessageNormalizerFactory(normalizers);

        factory.GetNormalizer("binance")
            .Should()
            .BeOfType<BinanceMessageNormalizer>();
    }

    [Fact]
    public void GetNormalizer_ShouldReturnNullForUnknownExchange()
    {
        var factory = new MessageNormalizerFactory([]);

        factory.GetNormalizer("unknown").Should().BeNull();
    }
}
