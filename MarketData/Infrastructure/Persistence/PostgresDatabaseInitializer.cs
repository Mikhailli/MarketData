using Dapper;
using MarketData.Application.Abstractions;
using MarketData.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MarketData.Infrastructure.Persistence;

public sealed class PostgresDatabaseInitializer(
    PostgresConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options,
    ILogger<PostgresDatabaseInitializer> logger) : IDatabaseInitializer
{
    private const string SchemaSql =
        """
        CREATE TABLE IF NOT EXISTS ticks
        (
            id BIGSERIAL PRIMARY KEY,
            exchange TEXT NOT NULL,
            symbol TEXT NOT NULL,
            price NUMERIC(18,8) NOT NULL,
            volume NUMERIC(18,8) NOT NULL,
            timestamp_utc TIMESTAMPTZ NOT NULL,
            received_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );

        CREATE UNIQUE INDEX IF NOT EXISTS ux_ticks_dedup
            ON ticks(exchange, symbol, price, volume, timestamp_utc);

        CREATE INDEX IF NOT EXISTS ix_ticks_symbol_timestamp
            ON ticks(symbol, timestamp_utc DESC);
        """;

    private readonly PostgresConnectionFactory _connectionFactory = connectionFactory;
    private readonly DatabaseOptions _options = options.Value;
    private readonly ILogger<PostgresDatabaseInitializer> _logger = logger;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnsureCreated)
        {
            _logger.LogInformation("Database schema initialization is disabled");
            return;
        }

        var attempts = Math.Max(1, _options.InitializationRetryCount);
        var delay = TimeSpan.FromMilliseconds(Math.Max(100, _options.InitializationRetryDelayMilliseconds));

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await using var connection = _connectionFactory.Create();
                await connection.OpenAsync(cancellationToken);

                await connection.ExecuteAsync(new CommandDefinition(
                    SchemaSql,
                    cancellationToken: cancellationToken));

                _logger.LogInformation("Database schema is ready");
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                _logger.LogWarning(
                    ex,
                    "Database initialization attempt {Attempt}/{Attempts} failed. Retrying in {Delay}",
                    attempt,
                    attempts,
                    delay);

                await Task.Delay(delay, cancellationToken);
            }
        }

        await using var finalConnection = _connectionFactory.Create();
        await finalConnection.OpenAsync(cancellationToken);
        await finalConnection.ExecuteAsync(new CommandDefinition(
            SchemaSql,
            cancellationToken: cancellationToken));
    }
}
