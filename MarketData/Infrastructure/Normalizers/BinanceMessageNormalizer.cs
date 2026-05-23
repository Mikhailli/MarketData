using System.Text.Json;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Normalizers;

public sealed class BinanceMessageNormalizer : IMessageNormalizer
{
    public string Exchange => "Binance";

    public bool TryNormalize(
        RawExchangeMessage raw,
        out IReadOnlyCollection<Tick> ticks)
    {
        ticks = [];

        try
        {
            using var document = JsonDocument.Parse(raw.Payload);
            var root = document.RootElement;

            if (!JsonElementReader.TryGetString(root, "s", out var symbol)
                || !JsonElementReader.TryGetDecimal(root, "p", out var price)
                || !JsonElementReader.TryGetDecimal(root, "q", out var volume)
                || !JsonElementReader.TryGetTimestampUtc(root, "T", out var timestampUtc))
            {
                return false;
            }

            ticks =
            [
                new Tick
                {
                    Exchange = raw.Exchange,
                    Symbol = symbol.ToUpperInvariant(),
                    Price = price,
                    Volume = volume,
                    TimestampUtc = timestampUtc
                }
            ];

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
