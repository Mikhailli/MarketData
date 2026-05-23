using MarketData.Application.Abstractions;
using MarketData.Infrastructure.Options;

namespace MarketData.Infrastructure.Exchanges;

public sealed class KrakenWebSocketClient(
    IConfiguration configuration,
    IMarketDataMetrics metrics,
    ILogger<KrakenWebSocketClient> logger) : WebSocketExchangeClientBase(
        "Kraken",
        ExchangeClientOptionsReader.Read(configuration, "Kraken", DefaultUrl, DefaultSubscriptions),
        metrics,
        logger)
{
    private const string DefaultUrl = "wss://ws.kraken.com/v2";

    private static readonly string[] DefaultSubscriptions =
    [
        """{"method":"subscribe","params":{"channel":"trade","symbol":["BTC/USD"]}}"""
    ];
}
