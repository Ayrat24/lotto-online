namespace MiniApp.Data;

public sealed class ReferralProgramSettings
{
    public int Id { get; set; } = 1;

    public bool Enabled { get; set; } = false;

    public decimal InviterBonusAmount { get; set; } = 1m;

    public decimal InviteeBonusAmount { get; set; } = 1m;

    public decimal MinQualifyingDepositAmount { get; set; } = 10m;

    public int EligibilityWindowDays { get; set; } = 30;

    public decimal MonthlyInviterBonusCap { get; set; } = 100m;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string? UpdatedByAdmin { get; set; }
}

