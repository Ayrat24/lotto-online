using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace MiniApp.Data;

public static class DatabaseModule
{
    public static IServiceCollection AddPostgresDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"];
        var isDevelopment = string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);

        services
            .AddOptions<DbOptions>()
            .Bind(configuration.GetSection(DbOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), $"{DbOptions.SectionName}:ConnectionString is required")
            .Validate(o => TryParseConnectionString(o.ConnectionString, out _), $"{DbOptions.SectionName}:ConnectionString must be a valid PostgreSQL connection string")
            .Validate(
                o => isDevelopment || HasPassword(o.ConnectionString),
                $"{DbOptions.SectionName}:ConnectionString must include a database password outside Development")
            .Validate(
                o => isDevelopment || !UsesDevelopmentPassword(o.ConnectionString),
                $"{DbOptions.SectionName}:ConnectionString must not use the default development password outside Development")
            .ValidateOnStart();

        services.AddDbContextPool<AppDbContext>((sp, options) =>
        {
            var db = sp.GetRequiredService<IOptions<DbOptions>>().Value;
            options.UseNpgsql(db.ConnectionString);
        });

        // Data access services
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();

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

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}

