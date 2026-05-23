namespace MarketData.Domain.Entities;

public sealed record RawExchangeMessage
{
    public required string Exchange { get; init; }

    public required string Payload { get; init; }

    public DateTime ReceivedUtc { get; init; } = DateTime.UtcNow;
}
