using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;

namespace MiniApp.Pages.Admin.Users;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public IndexModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public List<MiniAppUser> Users { get; private set; } = new();

    [BindProperty]
    public decimal FakeUserBalance { get; set; } = 25m;

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public string? StatusMessage { get; private set; }

    public bool StatusIsError { get; private set; }

    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            StatusMessage = FlashMessage;
            StatusIsError = FlashIsError ?? false;
        }

        if (LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, HttpContext.RequestAborted);

        Users = await _db.Users
            .OrderByDescending(x => x.LastSeenAtUtc)
            .Take(200)
            .ToListAsync();

        var changed = false;
        foreach (var user in Users)
        {
            if (!string.IsNullOrWhiteSpace(user.InviteCode))
                continue;

            user.InviteCode = await GenerateInviteCodeAsync(HttpContext.RequestAborted);
            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync(HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateFakeAsync(CancellationToken ct)
    {
        var balance = decimal.Round(Math.Max(0m, FakeUserBalance), 2, MidpointRounding.AwayFromZero);
        var nextTelegramUserId = (await _db.Users
            .Where(x => x.TelegramUserId >= 900000000)
            .Select(x => (long?)x.TelegramUserId)
            .MaxAsync(ct) ?? 900000000) + 1;

        var now = DateTimeOffset.UtcNow;
        var fake = new MiniAppUser
        {
            TelegramUserId = nextTelegramUserId,
            Number = $"+1999{nextTelegramUserId}",
            PreferredLanguage = "en",
            InviteCode = await GenerateInviteCodeAsync(ct),
            ReferredByUserIdOrUnbound = MiniAppUser.UnboundReferralUserId,
            IsFake = true,
            Balance = balance,
            CreatedAtUtc = now,
            LastSeenAtUtc = now
        };

        _db.Users.Add(fake);
        await _db.SaveChangesAsync(ct);

        FlashMessage = $"Fake user created (TelegramUserId={fake.TelegramUserId}, Balance={fake.Balance:0.00}).";
        FlashIsError = false;
        return RedirectToPage();
    }

    private async Task<string> GenerateInviteCodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < 16; i++)
        {
            var candidate = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var exists = await _db.Users.AsNoTracking().AnyAsync(x => x.InviteCode == candidate, ct);
            if (!exists)
                return candidate;
        }

        return Guid.NewGuid().ToString("N").ToUpperInvariant();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id, CancellationToken ct)
    {
        var u = await _db.Users.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (u is null)
            return RedirectToPage();

        _db.Users.Remove(u);
        await _db.SaveChangesAsync(ct);

        return RedirectToPage();
    }
}
