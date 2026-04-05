using Microsoft.EntityFrameworkCore;

namespace MiniApp.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<MiniAppUser> Users => Set<MiniAppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Draw> Draws => Set<Draw>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MiniAppUser>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);

            b.HasIndex(x => x.TelegramUserId).IsUnique();

            b.Property(x => x.TelegramUserId).IsRequired();
            b.Property(x => x.Number).HasMaxLength(64);
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.LastSeenAtUtc).IsRequired();
        });

        modelBuilder.Entity<Draw>(b =>
        {
            b.ToTable("draws");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).ValueGeneratedNever();

            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => x.State);

            b.Property(x => x.PrizePoolMatch3).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.PrizePoolMatch4).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.PrizePoolMatch5).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.State)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.Numbers).HasMaxLength(64);
            b.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<Ticket>(b =>
        {
            b.ToTable("tickets");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Draw)
                .WithMany()
                .HasForeignKey(x => x.DrawId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(x => new { x.UserId, x.PurchasedAtUtc });
            b.HasIndex(x => new { x.DrawId, x.PurchasedAtUtc });

            b.Property(x => x.DrawId).IsRequired();
            b.Property(x => x.Numbers).HasMaxLength(64).IsRequired();
            b.Property(x => x.PurchasedAtUtc).IsRequired();
        });
    }
}
