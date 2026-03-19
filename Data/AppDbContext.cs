using Microsoft.EntityFrameworkCore;

namespace MiniApp.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<MiniAppUser> Users => Set<MiniAppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

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

        modelBuilder.Entity<Ticket>(b =>
        {
            b.ToTable("tickets");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.UserId, x.PurchasedAtUtc });

            b.Property(x => x.Numbers).HasMaxLength(64).IsRequired();
            b.Property(x => x.PurchasedAtUtc).IsRequired();
        });
    }
}
