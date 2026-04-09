namespace MiniApp.Features.Referrals;

public sealed record ReferralProfileRequest(string InitData);

public sealed record ReferralBindRequest(string InitData, string InviteCode);

public sealed record ReferralProfileDto(
    string InviteCode,
    string InviteLink,
    bool IsBound,
    decimal TotalInviterRewards,
    decimal TotalInviteeRewards,
    int SuccessfulInvites,
    decimal MonthInviterRewards,
    decimal MonthInviterCap,
    bool MonthCapReached);

