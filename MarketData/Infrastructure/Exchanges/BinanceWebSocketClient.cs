using MarketData.Application.Abstractions;
using MarketData.Infrastructure.Options;

namespace MarketData.Infrastructure.Exchanges;

public sealed class BinanceWebSocketClient(
    IConfiguration configuration,
    IMarketDataMetrics metrics,
    ILogger<BinanceWebSocketClient> logger) : WebSocketExchangeClientBase(
        "Binance",
        ExchangeClientOptionsReader.Read(configuration, "Binance", DefaultUrl),
        metrics,
        logger)
{
    private const string DefaultUrl = "wss://stream.binance.com:9443/ws/btcusdt@trade";
}
