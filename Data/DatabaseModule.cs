using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MiniApp.Data;

public static class DatabaseModule
{
    public static IServiceCollection AddPostgresDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DbOptions>()
            .Bind(configuration.GetSection(DbOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), $"{DbOptions.SectionName}:ConnectionString is required")
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

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}

