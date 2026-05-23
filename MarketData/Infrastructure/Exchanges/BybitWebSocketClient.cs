using MarketData.Application.Abstractions;
using MarketData.Infrastructure.Options;

namespace MarketData.Infrastructure.Exchanges;

public sealed class BybitWebSocketClient(
    IConfiguration configuration,
    IMarketDataMetrics metrics,
    ILogger<BybitWebSocketClient> logger) : WebSocketExchangeClientBase(
        "Bybit",
        ExchangeClientOptionsReader.Read(configuration, "Bybit", DefaultUrl, DefaultSubscriptions),
        metrics,
        logger)
{
    private const string DefaultUrl = "wss://stream.bybit.com/v5/public/spot";

    private static readonly string[] DefaultSubscriptions =
    [
        """{"op":"subscribe","args":["publicTrade.BTCUSDT"]}"""
    ];
}
