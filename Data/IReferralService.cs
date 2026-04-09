namespace MiniApp.Data;

public interface IReferralService
{
    Task<ReferralProgramSettings> GetSettingsAsync(CancellationToken ct);

    Task<ReferralProgramSettings> SaveSettingsAsync(
        bool enabled,
        decimal inviterBonusAmount,
        decimal inviteeBonusAmount,
        decimal minQualifyingDepositAmount,
        int eligibilityWindowDays,
        decimal monthlyInviterBonusCap,
        string? updatedByAdmin,
        CancellationToken ct);

    Task<ReferralProfileResult> GetProfileAsync(long userId, CancellationToken ct);

    Task<ReferralBindResult> BindByCodeAsync(long inviteeUserId, string inviteCode, CancellationToken ct);

    Task ApplyBonusesForDepositAsync(CryptoDepositIntent deposit, DateTimeOffset now, CancellationToken ct);
}

public sealed record ReferralProfileResult(
    string InviteCode,
    long? ReferredByUserId,
    DateTimeOffset? ReferredAtUtc,
    decimal TotalInviterRewards,
    decimal TotalInviteeRewards,
    int SuccessfulInvites,
    decimal MonthInviterRewards,
    decimal MonthInviterCap);

public sealed record ReferralBindResult(bool Success, string? Error);

