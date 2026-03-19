using Microsoft.EntityFrameworkCore;

namespace MiniApp.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<MiniAppUser> Users => Set<MiniAppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MiniAppUser>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);

            b.HasIndex(x => x.TelegramUserId).IsUnique();

            b.Property(x => x.TelegramUserId).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.LastSeenAtUtc).IsRequired();
        });
    }
}

