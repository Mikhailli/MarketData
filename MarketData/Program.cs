using System.Threading.Channels;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;
using MarketData.Infrastructure.Exchanges;
using MarketData.Infrastructure.Metrics;
using MarketData.Infrastructure.Normalizers;
using MarketData.Infrastructure.Options;
using MarketData.Infrastructure.Persistence;
using MarketData.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext();
});

var pipelineOptions = builder.Configuration
    .GetSection(PipelineOptions.SectionName)
    .Get<PipelineOptions>() ?? new PipelineOptions();

builder.Services.AddSingleton(Options.Create(pipelineOptions));
builder.Services.AddSingleton(Options.Create(
    builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
    ?? new DatabaseOptions()));
builder.Services.AddSingleton(Options.Create(
    builder.Configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>()
    ?? new PersistenceOptions()));
builder.Services.AddSingleton(Options.Create(
    builder.Configuration.GetSection(DeduplicationOptions.SectionName).Get<DeduplicationOptions>()
    ?? new DeduplicationOptions()));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton(_ =>
    Channel.CreateBounded<RawExchangeMessage>(new BoundedChannelOptions(pipelineOptions.RawChannelCapacity)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = false,
        SingleWriter = false
    }));

builder.Services.AddSingleton(_ =>
    Channel.CreateBounded<Tick>(new BoundedChannelOptions(pipelineOptions.TickChannelCapacity)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    }));

builder.Services.AddSingleton<IMarketDataMetrics, MarketDataMetrics>();
builder.Services.AddSingleton<ITickDeduplicator, TickDeduplicator>();

builder.Services.AddSingleton<IMessageNormalizer, BinanceMessageNormalizer>();
builder.Services.AddSingleton<IMessageNormalizer, BybitMessageNormalizer>();
builder.Services.AddSingleton<IMessageNormalizer, KrakenMessageNormalizer>();
builder.Services.AddSingleton<IMessageNormalizerFactory, MessageNormalizerFactory>();

builder.Services.AddSingleton<PostgresConnectionFactory>();
builder.Services.AddSingleton<IDatabaseInitializer, PostgresDatabaseInitializer>();
builder.Services.AddSingleton<ITickRepository, PostgresTickRepository>();

builder.Services.AddSingleton<IWebSocketExchangeClient, BinanceWebSocketClient>();
builder.Services.AddSingleton<IWebSocketExchangeClient, BybitWebSocketClient>();
builder.Services.AddSingleton<IWebSocketExchangeClient, KrakenWebSocketClient>();

builder.Services.AddHostedService<DatabaseInitializerHostedService>();
builder.Services.AddHostedService<ExchangeHostedService>();
builder.Services.AddHostedService<TickProcessingService>();
builder.Services.AddHostedService<TickPersistenceService>();
builder.Services.AddHostedService<MetricsHostedService>();

var host = builder.Build();
await host.RunAsync();
