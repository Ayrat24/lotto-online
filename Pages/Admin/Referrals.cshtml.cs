using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class ReferralsModel : PageModel
{
    public sealed record RewardRow(
        long Id,
        DateTimeOffset CreatedAtUtc,
        string Type,
        decimal Amount,
        long InviterUserId,
        long InviterTelegramUserId,
        long InviteeUserId,
        long InviteeTelegramUserId,
        long RecipientUserId,
        long RecipientTelegramUserId,
        long DepositIntentId);

    public sealed record ProgramSettingsView(
        bool Enabled,
        decimal InviterBonusAmount,
        decimal InviteeBonusAmount,
        decimal MinQualifyingDepositAmount,
        int EligibilityWindowDays,
        decimal MonthlyInviterBonusCap,
        DateTimeOffset UpdatedAtUtc,
        string? UpdatedByAdmin);

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IReferralService _referrals;

    public ReferralsModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env, IReferralService referrals)
    {
        _db = db;
        _config = config;
        _env = env;
        _referrals = referrals;
    }

    [BindProperty]
    public bool Enabled { get; set; }

    [BindProperty]
    public decimal InviterBonusAmount { get; set; }

    [BindProperty]
    public decimal InviteeBonusAmount { get; set; }

    [BindProperty]
    public decimal MinQualifyingDepositAmount { get; set; }

    [BindProperty]
    public int EligibilityWindowDays { get; set; }

    [BindProperty]
    public decimal MonthlyInviterBonusCap { get; set; }

    public ProgramSettingsView Settings { get; private set; } = new(false, 0m, 0m, 0m, 0, 0m, DateTimeOffset.MinValue, null);

    public IReadOnlyList<RewardRow> RecentRewards { get; private set; } = Array.Empty<RewardRow>();

    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            StatusMessage = FlashMessage;
            StatusIsError = FlashIsError ?? false;
        }

        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (InviterBonusAmount < 0m || InviteeBonusAmount < 0m || MinQualifyingDepositAmount < 0m || MonthlyInviterBonusCap < 0m || EligibilityWindowDays < 0)
        {
            FlashMessage = "All referral settings must be zero or greater.";
            FlashIsError = true;
            return RedirectToPage();
        }

        await _referrals.SaveSettingsAsync(
            Enabled,
            InviterBonusAmount,
            InviteeBonusAmount,
            MinQualifyingDepositAmount,
            EligibilityWindowDays,
            MonthlyInviterBonusCap,
            User.Identity?.Name ?? "admin",
            ct);

        FlashMessage = "Referral settings were saved.";
        FlashIsError = false;
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        if (LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, ct);

        var settings = await _referrals.GetSettingsAsync(ct);
        Settings = new ProgramSettingsView(
            settings.Enabled,
            settings.InviterBonusAmount,
            settings.InviteeBonusAmount,
            settings.MinQualifyingDepositAmount,
            settings.EligibilityWindowDays,
            settings.MonthlyInviterBonusCap,
            settings.UpdatedAtUtc,
            settings.UpdatedByAdmin);

        Enabled = settings.Enabled;
        InviterBonusAmount = settings.InviterBonusAmount;
        InviteeBonusAmount = settings.InviteeBonusAmount;
        MinQualifyingDepositAmount = settings.MinQualifyingDepositAmount;
        EligibilityWindowDays = settings.EligibilityWindowDays;
        MonthlyInviterBonusCap = settings.MonthlyInviterBonusCap;

        RecentRewards = await _db.ReferralRewards
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(300)
            .Select(x => new RewardRow(
                x.Id,
                x.CreatedAtUtc,
                x.Type.ToString(),
                x.Amount,
                x.InviterUserId,
                x.InviterUser.TelegramUserId,
                x.InviteeUserId,
                x.InviteeUser.TelegramUserId,
                x.RecipientUserId,
                x.RecipientUser.TelegramUserId,
                x.DepositIntentId))
            .ToArrayAsync(ct);
    }
}

