using Dapper;
using MarketData.Application.Abstractions;
using MarketData.Domain.Entities;

namespace MarketData.Infrastructure.Persistence;

public sealed class PostgresTickRepository : ITickRepository
{
    private const string InsertSql =
        """
        INSERT INTO ticks
        (
            exchange,
            symbol,
            price,
            volume,
            timestamp_utc
        )
        VALUES
        (
            @Exchange,
            @Symbol,
            @Price,
            @Volume,
            @TimestampUtc
        )
        ON CONFLICT (exchange, symbol, price, volume, timestamp_utc)
        DO NOTHING;
        """;

    private readonly PostgresConnectionFactory _connectionFactory;

    public PostgresTickRepository(PostgresConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> SaveBatchAsync(
        IReadOnlyCollection<Tick> ticks,
        CancellationToken cancellationToken)
    {
        if (ticks.Count == 0)
        {
            return 0;
        }

        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            ticks,
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return affectedRows;
    }
}
