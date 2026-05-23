using MarketData.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MarketData.Infrastructure.Persistence;

public sealed class PostgresConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(
        IConfiguration configuration,
        IOptions<DatabaseOptions> options)
    {
        var databaseOptions = options.Value;

        _connectionString = !string.IsNullOrWhiteSpace(databaseOptions.ConnectionString)
            ? databaseOptions.ConnectionString
            : configuration.GetConnectionString(databaseOptions.ConnectionStringName)
              ?? throw new InvalidOperationException(
                  $"Connection string '{databaseOptions.ConnectionStringName}' is not configured.");
    }

    public NpgsqlConnection Create()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
