using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniApp.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Design-time only: avoids full host startup checks while enabling migrations scaffolding.
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=miniapp;Username=miniapp;Password=miniapp");

        return new AppDbContext(optionsBuilder.Options);
    }
}

