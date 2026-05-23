namespace MarketData.Domain.Entities;

public sealed record Tick
{
    public required string Exchange { get; init; }

    public required string Symbol { get; init; }

    public required decimal Price { get; init; }

    public required decimal Volume { get; init; }

    public required DateTime TimestampUtc { get; init; }
}
