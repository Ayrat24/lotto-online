using Microsoft.EntityFrameworkCore;

namespace MiniApp.Data;

public sealed class ReferralService : IReferralService
{
    private readonly AppDbContext _db;
    private readonly IWalletService _wallet;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(AppDbContext db, IWalletService wallet, ILogger<ReferralService> logger)
    {
        _db = db;
        _wallet = wallet;
        _logger = logger;
    }

    public async Task<ReferralProgramSettings> GetSettingsAsync(CancellationToken ct)
    {
        var settings = await _db.ReferralProgramSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (settings is not null)
            return settings;

        settings = new ReferralProgramSettings();
        _db.ReferralProgramSettings.Add(settings);
        await _db.SaveChangesAsync(ct);
        return settings;
    }

    public async Task<ReferralProgramSettings> SaveSettingsAsync(
        bool enabled,
        decimal inviterBonusAmount,
        decimal inviteeBonusAmount,
        decimal minQualifyingDepositAmount,
        int eligibilityWindowDays,
        decimal monthlyInviterBonusCap,
        string? updatedByAdmin,
        CancellationToken ct)
    {
        var settings = await GetSettingsAsync(ct);

        settings.Enabled = enabled;
        settings.InviterBonusAmount = RoundMoney(Math.Max(0m, inviterBonusAmount));
        settings.InviteeBonusAmount = RoundMoney(Math.Max(0m, inviteeBonusAmount));
        settings.MinQualifyingDepositAmount = RoundMoney(Math.Max(0m, minQualifyingDepositAmount));
        settings.EligibilityWindowDays = Math.Max(0, eligibilityWindowDays);
        settings.MonthlyInviterBonusCap = RoundMoney(Math.Max(0m, monthlyInviterBonusCap));
        settings.UpdatedByAdmin = string.IsNullOrWhiteSpace(updatedByAdmin) ? "admin" : updatedByAdmin.Trim();
        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return settings;
    }

    public async Task<ReferralProfileResult> GetProfileAsync(long userId, CancellationToken ct)
    {
        var user = await _db.Users.SingleAsync(x => x.Id == userId, ct);
        if (string.IsNullOrWhiteSpace(user.InviteCode))
        {
            user.InviteCode = await GenerateInviteCodeAsync(ct);
            await _db.SaveChangesAsync(ct);
        }

        var totalInviterRewards = await _db.ReferralRewards
            .AsNoTracking()
            .Where(x => x.RecipientUserId == userId && x.Type == ReferralRewardType.InviterBonus)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var totalInviteeRewards = await _db.ReferralRewards
            .AsNoTracking()
            .Where(x => x.RecipientUserId == userId && x.Type == ReferralRewardType.InviteeBonus)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var successfulInvites = await _db.ReferralRewards
            .AsNoTracking()
            .Where(x => x.InviterUserId == userId && x.Type == ReferralRewardType.InviterBonus)
            .Select(x => x.InviteeUserId)
            .Distinct()
            .CountAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1);

        var monthInviterRewards = await _db.ReferralRewards
            .AsNoTracking()
            .Where(x => x.RecipientUserId == userId
                && x.Type == ReferralRewardType.InviterBonus
                && x.CreatedAtUtc >= monthStart
                && x.CreatedAtUtc < monthEnd)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var settings = await GetSettingsAsync(ct);

        return new ReferralProfileResult(
            user.InviteCode!,
            user.ReferredByUserId,
            user.ReferredAtUtc,
            totalInviterRewards,
            totalInviteeRewards,
            successfulInvites,
            monthInviterRewards,
            settings.MonthlyInviterBonusCap);
    }

    public async Task<ReferralBindResult> BindByCodeAsync(long inviteeUserId, string inviteCode, CancellationToken ct)
    {
        var normalizedCode = NormalizeInviteCode(inviteCode);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            _logger.LogWarning("Referral bind failed: code is invalid after normalization. InviteeUserId={InviteeUserId}", inviteeUserId);
            return new ReferralBindResult(false, "Invite code is invalid.");
        }

        var invitee = await _db.Users.SingleAsync(x => x.Id == inviteeUserId, ct);

        var inviter = await _db.Users.SingleOrDefaultAsync(x => x.InviteCode == normalizedCode, ct);
        if (inviter is null)
        {
            _logger.LogWarning("Referral bind failed: invite code not found. InviteeUserId={InviteeUserId}, Code={Code}", inviteeUserId, normalizedCode);
            return new ReferralBindResult(false, "Invite code was not found.");
        }

        if (inviter.Id == invitee.Id)
        {
            _logger.LogWarning("Referral bind failed: self-referral attempt. InviteeUserId={InviteeUserId}, Code={Code}", inviteeUserId, normalizedCode);
            return new ReferralBindResult(false, "You cannot use your own invite code.");
        }

        // Debug mode behavior: allow re-binding to simplify promo flow diagnostics.
        invitee.ReferredByUserId = inviter.Id;
        invitee.ReferredAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Referral bind succeeded. InviteeUserId={InviteeUserId}, InviterUserId={InviterUserId}, Code={Code}", invitee.Id, inviter.Id, normalizedCode);

        return new ReferralBindResult(true, null);
    }

    public async Task<ReferralCodeCheckResult> CheckCodeAsync(long inviteeUserId, string inviteCode, CancellationToken ct)
    {
        var normalizedCode = NormalizeInviteCode(inviteCode);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            _logger.LogWarning("Referral check failed: code is invalid after normalization. InviteeUserId={InviteeUserId}", inviteeUserId);
            return new ReferralCodeCheckResult(false, "Invite code is invalid.");
        }

        var invitee = await _db.Users.SingleAsync(x => x.Id == inviteeUserId, ct);

        var inviter = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.InviteCode == normalizedCode, ct);
        if (inviter is null)
        {
            _logger.LogWarning("Referral check failed: invite code not found. InviteeUserId={InviteeUserId}, Code={Code}", inviteeUserId, normalizedCode);
            return new ReferralCodeCheckResult(false, "Invite code was not found.");
        }

        if (inviter.Id == invitee.Id)
        {
            _logger.LogWarning("Referral check failed: self-referral attempt. InviteeUserId={InviteeUserId}, Code={Code}", inviteeUserId, normalizedCode);
            return new ReferralCodeCheckResult(false, "You cannot use your own invite code.");
        }

        _logger.LogInformation("Referral check succeeded. InviteeUserId={InviteeUserId}, InviterUserId={InviterUserId}, Code={Code}", invitee.Id, inviter.Id, normalizedCode);
        return new ReferralCodeCheckResult(true, null, inviter.Id);
    }

    public async Task ApplyBonusesForDepositAsync(CryptoDepositIntent deposit, DateTimeOffset now, CancellationToken ct)
    {
        if (deposit.CreditedAtUtc is null)
            return;

        var settings = await GetSettingsAsync(ct);
        if (!settings.Enabled)
            return;

        if (settings.InviterBonusAmount <= 0m && settings.InviteeBonusAmount <= 0m)
            return;

        if (deposit.Amount < settings.MinQualifyingDepositAmount)
            return;

        var invitee = await _db.Users.SingleAsync(x => x.Id == deposit.UserId, ct);
        if (!invitee.ReferredByUserId.HasValue)
            return;

        var inviterId = invitee.ReferredByUserId.Value;
        if (inviterId == invitee.Id)
            return;

        var hasAnyPreviousCreditedDeposit = await _db.CryptoDepositIntents
            .AsNoTracking()
            .AnyAsync(x => x.UserId == invitee.Id && x.CreditedAtUtc != null && x.Id != deposit.Id, ct);
        if (hasAnyPreviousCreditedDeposit)
            return;

        var hasExistingRewards = await _db.ReferralRewards
            .AsNoTracking()
            .AnyAsync(x => x.DepositIntentId == deposit.Id, ct);
        if (hasExistingRewards)
            return;

        if (settings.EligibilityWindowDays > 0)
        {
            var eligibilityEndsAtUtc = invitee.CreatedAtUtc.AddDays(settings.EligibilityWindowDays);
            if (now > eligibilityEndsAtUtc)
                return;
        }

        var inviter = await _db.Users.SingleOrDefaultAsync(x => x.Id == inviterId, ct);
        if (inviter is null)
            return;

        var inviterBonusAmount = RoundMoney(settings.InviterBonusAmount);
        var inviteeBonusAmount = RoundMoney(settings.InviteeBonusAmount);

        if (settings.MonthlyInviterBonusCap > 0m && inviterBonusAmount > 0m)
        {
            var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var monthEnd = monthStart.AddMonths(1);

            var monthTotal = await _db.ReferralRewards
                .AsNoTracking()
                .Where(x => x.RecipientUserId == inviterId
                    && x.Type == ReferralRewardType.InviterBonus
                    && x.CreatedAtUtc >= monthStart
                    && x.CreatedAtUtc < monthEnd)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

            if (monthTotal >= settings.MonthlyInviterBonusCap)
            {
                inviterBonusAmount = 0m;
            }
            else if (monthTotal + inviterBonusAmount > settings.MonthlyInviterBonusCap)
            {
                inviterBonusAmount = RoundMoney(settings.MonthlyInviterBonusCap - monthTotal);
            }
        }

        var totalBonus = inviterBonusAmount + inviteeBonusAmount;
        if (totalBonus <= 0m)
            return;

        var serverWallet = await _wallet.EnsureServerWalletAsync(ct);
        if (serverWallet.Balance < totalBonus)
            return;

        var serverBalanceBeforeBonuses = serverWallet.Balance;

        if (inviterBonusAmount > 0m)
        {
            inviter.Balance = RoundMoney(inviter.Balance + inviterBonusAmount);
            serverBalanceBeforeBonuses = RoundMoney(serverBalanceBeforeBonuses - inviterBonusAmount);
            _db.WalletTransactions.Add(new WalletTransaction
            {
                UserId = inviter.Id,
                Type = WalletTransactionType.ReferralInviterBonus,
                UserDelta = inviterBonusAmount,
                UserBalanceAfter = inviter.Balance,
                ServerDelta = -inviterBonusAmount,
                ServerBalanceAfter = serverBalanceBeforeBonuses,
                Reference = $"referral:deposit:{deposit.Id}:inviter",
                CreatedAtUtc = now
            });

            _db.ReferralRewards.Add(new ReferralReward
            {
                InviterUserId = inviter.Id,
                InviteeUserId = invitee.Id,
                RecipientUserId = inviter.Id,
                DepositIntentId = deposit.Id,
                Type = ReferralRewardType.InviterBonus,
                Amount = inviterBonusAmount,
                CreatedAtUtc = now
            });
        }

        if (inviteeBonusAmount > 0m)
        {
            invitee.Balance = RoundMoney(invitee.Balance + inviteeBonusAmount);
            serverBalanceBeforeBonuses = RoundMoney(serverBalanceBeforeBonuses - inviteeBonusAmount);
            _db.WalletTransactions.Add(new WalletTransaction
            {
                UserId = invitee.Id,
                Type = WalletTransactionType.ReferralInviteeBonus,
                UserDelta = inviteeBonusAmount,
                UserBalanceAfter = invitee.Balance,
                ServerDelta = -inviteeBonusAmount,
                ServerBalanceAfter = serverBalanceBeforeBonuses,
                Reference = $"referral:deposit:{deposit.Id}:invitee",
                CreatedAtUtc = now
            });

            _db.ReferralRewards.Add(new ReferralReward
            {
                InviterUserId = inviter.Id,
                InviteeUserId = invitee.Id,
                RecipientUserId = invitee.Id,
                DepositIntentId = deposit.Id,
                Type = ReferralRewardType.InviteeBonus,
                Amount = inviteeBonusAmount,
                CreatedAtUtc = now
            });
        }

        serverWallet.Balance = serverBalanceBeforeBonuses;
        serverWallet.UpdatedAtUtc = now;
    }

    private async Task<string> GenerateInviteCodeAsync(CancellationToken ct)
    {
        // Retry on unlikely collisions so codes stay short and shareable.
        for (var i = 0; i < 16; i++)
        {
            var candidate = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var exists = await _db.Users.AsNoTracking().AnyAsync(x => x.InviteCode == candidate, ct);
            if (!exists)
                return candidate;
        }

        return Guid.NewGuid().ToString("N").ToUpperInvariant();
    }

    private static string NormalizeInviteCode(string value)
    {
        var chars = (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return new string(chars);
    }

    private static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}


