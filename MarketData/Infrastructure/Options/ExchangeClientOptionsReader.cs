using Microsoft.Extensions.Configuration;

namespace MarketData.Infrastructure.Options;

public static class ExchangeClientOptionsReader
{
    public static ExchangeClientOptions Read(
        IConfiguration configuration,
        string exchange,
        string defaultUrl,
        IReadOnlyCollection<string>? defaultSubscriptions = null)
    {
        var options = new ExchangeClientOptions
        {
            Url = defaultUrl,
            Subscriptions = defaultSubscriptions?.ToList() ?? []
        };

        configuration.GetSection($"Exchanges:{exchange}").Bind(options);

        if (string.IsNullOrWhiteSpace(options.Url))
        {
            options.Url = defaultUrl;
        }

        if (options.ReconnectDelaySeconds <= 0)
        {
            options.ReconnectDelaySeconds = 5;
        }

        if (options.ReceiveBufferSize < 1024)
        {
            options.ReceiveBufferSize = 8 * 1024;
        }

        return options;
    }
}
