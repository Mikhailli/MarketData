namespace MarketData.Infrastructure.Options;

public sealed class ExchangeClientOptions
{
    public bool Enabled { get; set; } = true;

    public required string Url { get; set; }

    public int ReconnectDelaySeconds { get; set; } = 5;

    public int ReceiveBufferSize { get; set; } = 8 * 1024;

    public List<string> Subscriptions { get; set; } = [];
}
