using MarketData.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketData.Infrastructure.Services;

public sealed class DatabaseInitializerHostedService(
    IDatabaseInitializer databaseInitializer,
    ILogger<DatabaseInitializerHostedService> logger) : IHostedService
{
    private readonly IDatabaseInitializer _databaseInitializer = databaseInitializer;
    private readonly ILogger<DatabaseInitializerHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing database schema");
        await _databaseInitializer.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
