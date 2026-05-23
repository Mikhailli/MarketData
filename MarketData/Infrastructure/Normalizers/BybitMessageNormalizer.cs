using System.Text.Json;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Normalizers;

public sealed class BybitMessageNormalizer : IMessageNormalizer
{
    public string Exchange => "Bybit";

    public bool TryNormalize(
        RawExchangeMessage raw,
        out IReadOnlyCollection<Tick> ticks)
    {
        ticks = [];

        try
        {
            using var document = JsonDocument.Parse(raw.Payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("data", out var data))
            {
                return false;
            }

            var normalized = new List<Tick>();

            if (data.ValueKind == JsonValueKind.Array)
            {
                foreach (var trade in data.EnumerateArray())
                {
                    if (TryReadTrade(raw.Exchange, trade, out var tick))
                    {
                        normalized.Add(tick);
                    }
                }
            }
            else if (data.ValueKind == JsonValueKind.Object
                     && TryReadTrade(raw.Exchange, data, out var tick))
            {
                normalized.Add(tick);
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

        if (!JsonElementReader.TryGetString(trade, "s", out var symbol)
            || !JsonElementReader.TryGetDecimal(trade, "p", out var price)
            || !JsonElementReader.TryGetDecimal(trade, "v", out var volume)
            || !JsonElementReader.TryGetTimestampUtc(trade, "T", out var timestampUtc))
        {
            return false;
        }

        tick = new Tick
        {
            Exchange = exchange,
            Symbol = symbol.ToUpperInvariant(),
            Price = price,
            Volume = volume,
            TimestampUtc = timestampUtc
        };

        return true;
    }
}
