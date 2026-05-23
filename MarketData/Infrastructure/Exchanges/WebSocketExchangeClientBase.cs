using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Options;

namespace MarketData.Infrastructure.Exchanges;

public abstract class WebSocketExchangeClientBase(
    string exchange,
    ExchangeClientOptions options,
    IMarketDataMetrics metrics,
    ILogger logger) : IWebSocketExchangeClient
{
    private readonly ILogger _logger = logger;
    private readonly IMarketDataMetrics _metrics = metrics;
    private readonly ExchangeClientOptions _options = options;

    public string Exchange { get; } = exchange;

    public async Task StartAsync(
        ChannelWriter<RawExchangeMessage> writer,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("{Exchange} websocket client is disabled", Exchange);
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();

            try
            {
                _logger.LogInformation("Connecting to {Exchange}: {Url}", Exchange, _options.Url);

                await socket.ConnectAsync(new Uri(_options.Url), cancellationToken);
                _logger.LogInformation("Connected to {Exchange}", Exchange);

                await OnConnectedAsync(socket, cancellationToken);
                await ReceiveLoopAsync(socket, writer, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Exchange} websocket connection failed", Exchange);
            }
            finally
            {
                await CloseQuietlyAsync(socket, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _metrics.IncrementReconnects(Exchange);
            var reconnectDelay = TimeSpan.FromSeconds(_options.ReconnectDelaySeconds);
            _logger.LogWarning("Reconnecting to {Exchange} in {Delay}", Exchange, reconnectDelay);
            await Task.Delay(reconnectDelay, cancellationToken);
        }
    }

    protected virtual async Task OnConnectedAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
    {
        foreach (var subscription in _options.Subscriptions)
        {
            if (string.IsNullOrWhiteSpace(subscription))
            {
                continue;
            }

            var bytes = Encoding.UTF8.GetBytes(subscription);
            await socket.SendAsync(
                bytes.AsMemory(),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);

            _logger.LogInformation(
                "Sent {Exchange} subscription: {Subscription}",
                Exchange,
                subscription);
        }
    }

    private async Task ReceiveLoopAsync(
        ClientWebSocket socket,
        ChannelWriter<RawExchangeMessage> writer,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);

        try
        {
            while (socket.State == WebSocketState.Open
                   && !cancellationToken.IsCancellationRequested)
            {
                using var payload = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer, 0, buffer.Length),
                        cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning(
                            "{Exchange} websocket closed by remote endpoint: {CloseStatus} {Description}",
                            Exchange,
                            result.CloseStatus,
                            result.CloseStatusDescription);
                        return;
                    }

                    payload.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    continue;
                }

                var message = Encoding.UTF8.GetString(payload.ToArray());

                await writer.WriteAsync(
                    new RawExchangeMessage
                    {
                        Exchange = Exchange,
                        Payload = message,
                        ReceivedUtc = DateTime.UtcNow
                    },
                    cancellationToken);

                _metrics.IncrementRawMessagesReceived(Exchange);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task CloseQuietlyAsync(
        ClientWebSocket socket,
        CancellationToken cancellationToken)
    {
        if (socket.State is not (WebSocketState.Open or WebSocketState.CloseReceived))
        {
            return;
        }

        try
        {
            using var closeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            closeCts.CancelAfter(TimeSpan.FromSeconds(2));

            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client stopped",
                closeCts.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Failed to gracefully close {Exchange} websocket", Exchange);
        }
    }
}
