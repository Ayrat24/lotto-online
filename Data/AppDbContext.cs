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
    public DbSet<CryptoDepositIntent> CryptoDepositIntents => Set<CryptoDepositIntent>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<LocalizationText> LocalizationTexts => Set<LocalizationText>();
    public DbSet<ReferralProgramSettings> ReferralProgramSettings => Set<ReferralProgramSettings>();
    public DbSet<ReferralReward> ReferralRewards => Set<ReferralReward>();
    public DbSet<NewsBanner> NewsBanners => Set<NewsBanner>();
    public DbSet<TicketPurchaseSettings> TicketPurchaseSettings => Set<TicketPurchaseSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MiniAppUser>(b =>
        {
            b.ToTable("users");
            b.HasKey(x => x.Id);

            b.HasIndex(x => x.TelegramUserId).IsUnique();
            b.HasIndex(x => x.AcquisitionDeepLink);
            b.HasIndex(x => x.InviteCode)
                .IsUnique()
                .HasFilter("\"InviteCode\" IS NOT NULL");

            b.Property(x => x.TelegramUserId).IsRequired();
            b.Property(x => x.Number).HasMaxLength(64);
            b.Property(x => x.PreferredLanguage).HasMaxLength(8);
            b.Property(x => x.AcquisitionDeepLink).HasMaxLength(128);
            b.Property(x => x.WalletAddress).HasMaxLength(256);
            b.Property(x => x.InviteCode).HasMaxLength(32);
            b.Property(x => x.IsFake).IsRequired().HasDefaultValue(false);
            b.Property(x => x.Balance).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.LastSeenAtUtc).IsRequired();

            b.HasOne<MiniAppUser>()
                .WithMany()
                .HasForeignKey(x => x.ReferredByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ReferralProgramSettings>(b =>
        {
            b.ToTable("referral_program_settings");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Enabled).IsRequired();
            b.Property(x => x.InviterBonusAmount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.InviteeBonusAmount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.MinQualifyingDepositAmount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.EligibilityWindowDays).IsRequired();
            b.Property(x => x.MonthlyInviterBonusCap).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.UpdatedByAdmin).HasMaxLength(128);
            b.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<TicketPurchaseSettings>(b =>
        {
            b.ToTable("ticket_purchase_settings");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.TicketSlotsCount).IsRequired();
            b.Property(x => x.UpdatedByAdmin).HasMaxLength(128);
            b.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<ReferralReward>(b =>
        {
            b.ToTable("referral_rewards");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.InviterUser)
                .WithMany()
                .HasForeignKey(x => x.InviterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.InviteeUser)
                .WithMany()
                .HasForeignKey(x => x.InviteeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.RecipientUser)
                .WithMany()
                .HasForeignKey(x => x.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.DepositIntent)
                .WithMany()
                .HasForeignKey(x => x.DepositIntentId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.DepositIntentId, x.Type }).IsUnique();
            b.HasIndex(x => new { x.RecipientUserId, x.Type, x.CreatedAtUtc });
            b.HasIndex(x => new { x.InviterUserId, x.CreatedAtUtc });

            b.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<LocalizationText>(b =>
        {
            b.ToTable("localization_texts");
            b.HasKey(x => x.Id);

            b.HasIndex(x => x.Key).IsUnique();
            b.HasIndex(x => x.UpdatedAtUtc);

            b.Property(x => x.Key).HasMaxLength(128).IsRequired();
            b.Property(x => x.EnglishValue).HasMaxLength(2048).IsRequired();
            b.Property(x => x.RussianValue).HasMaxLength(2048).IsRequired();
            b.Property(x => x.UzbekValue).HasMaxLength(2048).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<NewsBanner>(b =>
        {
            b.ToTable("news_banners");
            b.HasKey(x => x.Id);

            b.HasIndex(x => new { x.IsPublished, x.DisplayOrder, x.CreatedAtUtc });
            b.HasIndex(x => x.UpdatedAtUtc);

            b.Property(x => x.ImagePath).HasMaxLength(512).IsRequired();
            b.Property(x => x.ActionType).HasMaxLength(32).IsRequired().HasDefaultValue("none");
            b.Property(x => x.ActionValue).HasMaxLength(1024);
            b.Property(x => x.DisplayOrder).IsRequired();
            b.Property(x => x.IsPublished).IsRequired().HasDefaultValue(true);
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<Draw>(b =>
        {
            b.ToTable("draws");
            b.HasKey(x => x.Id);

            b.Property(x => x.Id).ValueGeneratedNever();

            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => x.PurchaseClosesAtUtc);
            b.HasIndex(x => x.State);

            b.Property(x => x.PrizePoolMatch3).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.PrizePoolMatch4).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.PrizePoolMatch5).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.TicketCost).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.State)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.PurchaseClosesAtUtc).IsRequired();
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
            b.HasIndex(x => new { x.UserId, x.DrawId, x.NumbersSignature })
                .IsUnique()
                .HasFilter("\"NumbersSignature\" IS NOT NULL");

            b.Property(x => x.DrawId).IsRequired();
            b.Property(x => x.Numbers).HasMaxLength(64).IsRequired();
            b.Property(x => x.NumbersSignature).HasMaxLength(64);
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
            b.HasIndex(x => x.ExternalPayoutId)
                .IsUnique()
                .HasFilter("\"ExternalPayoutId\" IS NOT NULL");

            b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Number).HasMaxLength(256).IsRequired();
            b.Property(x => x.ExternalPayoutId).HasMaxLength(128);
            b.Property(x => x.ExternalPayoutState).HasMaxLength(64);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.ReviewedByAdmin).HasMaxLength(128);
            b.Property(x => x.ReviewNote).HasMaxLength(256);
            b.Property(x => x.CreatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<CryptoDepositIntent>(b =>
        {
            b.ToTable("crypto_deposit_intents");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
            b.HasIndex(x => new { x.Provider, x.ProviderInvoiceId }).IsUnique();

            b.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(16).IsRequired();
            b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            b.Property(x => x.ProviderInvoiceId).HasMaxLength(128).IsRequired();
            b.Property(x => x.CheckoutLink).HasMaxLength(1024).IsRequired();
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.LastProviderEventType).HasMaxLength(128);
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<PaymentWebhookEvent>(b =>
        {
            b.ToTable("payment_webhook_events");
            b.HasKey(x => x.Id);

            b.HasIndex(x => x.ReceivedAtUtc);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => new { x.Provider, x.DeliveryId })
                .IsUnique()
                .HasFilter("\"DeliveryId\" IS NOT NULL");

            b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            b.Property(x => x.DeliveryId).HasMaxLength(128);
            b.Property(x => x.EventType).HasMaxLength(128);
            b.Property(x => x.ProviderObjectId).HasMaxLength(128);
            b.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            b.Property(x => x.Error).HasMaxLength(512);
            b.Property(x => x.ReceivedAtUtc).IsRequired();
        });
    }
}
