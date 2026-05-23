using System.Threading.Channels;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Services;

public sealed class ExchangeHostedService(
    IEnumerable<IWebSocketExchangeClient> clients,
    Channel<RawExchangeMessage> rawChannel,
    ILogger<ExchangeHostedService> logger) : BackgroundService
{
    private readonly IReadOnlyCollection<IWebSocketExchangeClient> _clients = clients.ToArray();
    private readonly Channel<RawExchangeMessage> _rawChannel = rawChannel;
    private readonly ILogger<ExchangeHostedService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting {Count} websocket exchange clients", _clients.Count);

        var tasks = _clients
            .Select(client => RunClientAsync(client, stoppingToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task RunClientAsync(
        IWebSocketExchangeClient client,
        CancellationToken stoppingToken)
    {
        try
        {
            await client.StartAsync(_rawChannel.Writer, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown path.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Exchange} client stopped unexpectedly", client.Exchange);
        }
    }
}
