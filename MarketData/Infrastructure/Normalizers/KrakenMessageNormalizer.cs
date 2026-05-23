using System.Text.Json;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Normalizers;

public sealed class KrakenMessageNormalizer : IMessageNormalizer
{
    public string Exchange => "Kraken";

    public bool TryNormalize(
        RawExchangeMessage raw,
        out IReadOnlyCollection<Tick> ticks)
    {
        ticks = [];

        try
        {
            using var document = JsonDocument.Parse(raw.Payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var normalized = new List<Tick>();

            foreach (var trade in data.EnumerateArray())
            {
                if (TryReadTrade(raw.Exchange, trade, out var tick))
                {
                    normalized.Add(tick);
                }
            }

            ticks = normalized;
            return normalized.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadTrade(
        string exchange,
        JsonElement trade,
        out Tick tick)
    {
        tick = null!;

        if (!JsonElementReader.TryGetString(trade, "symbol", out var symbol)
            || !JsonElementReader.TryGetDecimal(trade, "price", out var price)
            || !JsonElementReader.TryGetDecimal(trade, "qty", out var volume)
            || !JsonElementReader.TryGetTimestampUtc(trade, "timestamp", out var timestampUtc))
        {
            return false;
        }

        tick = new Tick
        {
            Exchange = exchange,
            Symbol = symbol.Replace("/", string.Empty, StringComparison.Ordinal).ToUpperInvariant(),
            Price = price,
            Volume = volume,
            TimestampUtc = timestampUtc
        };

        return true;
    }
}
