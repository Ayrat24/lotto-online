using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniApp.Data;
using Npgsql;

namespace MiniApp.Admin;

public sealed class DatabaseResetService
{
    private const string ResetPublicSchemaSql = """
                                              DROP SCHEMA IF EXISTS public CASCADE;
                                              CREATE SCHEMA public;
                                              GRANT ALL ON SCHEMA public TO public;
                                              """;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _connectionString;
    private readonly ILogger<DatabaseResetService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public DatabaseResetService(
        IServiceScopeFactory scopeFactory,
        IOptions<DbOptions> dbOptions,
        ILogger<DatabaseResetService> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionString = dbOptions.Value.ConnectionString;
        _logger = logger;
    }

    public async Task ResetPublicSchemaAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Database reset requires a PostgreSQL connection string.");

            await using (var providerScope = _scopeFactory.CreateAsyncScope())
            {
                var providerDb = providerScope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (!providerDb.Database.IsRelational())
                    throw new InvalidOperationException("Database reset is only available for relational database providers.");
            }

            _logger.LogWarning("Admin database reset started: dropping and recreating public schema.");

            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync(ct);

                await using var command = new NpgsqlCommand(ResetPublicSchemaSql, connection);
                await command.ExecuteNonQueryAsync(ct);
            }

            NpgsqlConnection.ClearAllPools();

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(ct);

            NpgsqlConnection.ClearAllPools();

            _logger.LogInformation("Admin database reset completed and migrations were reapplied.");
        }
        finally
        {
            _gate.Release();
        }
    }
}

