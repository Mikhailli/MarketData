using System.Threading.Channels;
using MarketData.Domain.Entities;

namespace MarketData.Application.Abstractions;

public interface IWebSocketExchangeClient
{
    string Exchange { get; }

    Task StartAsync(
        ChannelWriter<RawExchangeMessage> writer,
        CancellationToken cancellationToken);
}
