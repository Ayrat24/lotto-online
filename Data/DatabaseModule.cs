using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MiniApp.Data;

public static class DatabaseModule
{
    private static readonly InMemoryDatabaseRoot LocalDebugDbRoot = new();

    public static IServiceCollection AddPostgresDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"];
        var isDevelopment = string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);
        var localDebugEnabled = isDevelopment && configuration.GetValue<bool>("LocalDebug:Enabled");
        var configuredConnectionString = configuration.GetValue<string>($"{DbOptions.SectionName}:ConnectionString");
        var useInMemoryForLocalDebug = localDebugEnabled && string.IsNullOrWhiteSpace(configuredConnectionString);

        var optionsBuilder = services
            .AddOptions<DbOptions>()
            .Bind(configuration.GetSection(DbOptions.SectionName));

        if (!useInMemoryForLocalDebug)
        {
            optionsBuilder
                .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), $"{DbOptions.SectionName}:ConnectionString is required")
                .Validate(o => TryParseConnectionString(o.ConnectionString, out _), $"{DbOptions.SectionName}:ConnectionString must be a valid PostgreSQL connection string")
                .Validate(
                    o => isDevelopment || HasPassword(o.ConnectionString),
                    $"{DbOptions.SectionName}:ConnectionString must include a database password outside Development")
                .Validate(
                    o => isDevelopment || !UsesDevelopmentPassword(o.ConnectionString),
                    $"{DbOptions.SectionName}:ConnectionString must not use the default development password outside Development")
                .ValidateOnStart();
        }

        services.AddDbContextPool<AppDbContext>((sp, options) =>
        {
            if (useInMemoryForLocalDebug)
            {
                options.UseInMemoryDatabase("miniapp-local-debug", LocalDebugDbRoot);
                return;
            }

            var db = sp.GetRequiredService<IOptions<DbOptions>>().Value;
            options.UseNpgsql(db.ConnectionString);
        });

        // Data access services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IWalletService, WalletService>();

        return services;
    }

    private static bool TryParseConnectionString(string? connectionString, out NpgsqlConnectionStringBuilder? builder)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            builder = null;
            return false;
        }

        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
            return true;
        }
        catch
        {
            builder = null;
            return false;
        }
    }

    private static bool UsesDevelopmentPassword(string? connectionString)
        => TryParseConnectionString(connectionString, out var builder)
           && string.Equals(builder!.Password, "miniapp", StringComparison.Ordinal);

    private static bool HasPassword(string? connectionString)
        => TryParseConnectionString(connectionString, out var builder)
           && !string.IsNullOrWhiteSpace(builder!.Password);

    public static async Task<bool> ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Database.IsRelational())
            return false;

        await db.Database.MigrateAsync();
        return true;
    }
}

