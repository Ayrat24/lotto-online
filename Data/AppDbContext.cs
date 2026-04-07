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
    public DbSet<ServerWallet> ServerWallets => Set<ServerWallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();

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
            b.Property(x => x.Balance).HasPrecision(18, 2).IsRequired();
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
            b.Property(x => x.TicketCost).HasPrecision(18, 2).IsRequired();
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
            b.HasIndex(x => new { x.UserId, x.Status, x.DrawId });

            b.Property(x => x.DrawId).IsRequired();
            b.Property(x => x.Numbers).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.PurchasedAtUtc).IsRequired();
        });

        modelBuilder.Entity<ServerWallet>(b =>
        {
            b.ToTable("server_wallet");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Balance).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<WalletTransaction>(b =>
        {
            b.ToTable("wallet_transactions");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            b.HasIndex(x => x.Type);

            b.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.UserDelta).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.UserBalanceAfter).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.ServerDelta).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.ServerBalanceAfter).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Reference).HasMaxLength(128);
            b.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<WithdrawalRequest>(b =>
        {
            b.ToTable("withdrawal_requests");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
            b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });

            b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Number).HasMaxLength(64).IsRequired();
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.ReviewedByAdmin).HasMaxLength(128);
            b.Property(x => x.ReviewNote).HasMaxLength(256);
            b.Property(x => x.CreatedAtUtc).IsRequired();
        });
    }
}
