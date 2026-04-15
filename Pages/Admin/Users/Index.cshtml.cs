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

    [BindProperty(SupportsGet = true)]
    public string? SearchPhone { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTelegramId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchDeepLink { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "lastSeenAt";

    [BindProperty(SupportsGet = true)]
    public string SortDir { get; set; } = "desc";

    [BindProperty]
    public decimal FakeUserBalance { get; set; } = 25m;

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public string? StatusMessage { get; private set; }

    public bool StatusIsError { get; private set; }

    public string GetNextSortDir(string column)
        => string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase)
           && string.Equals(SortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : "asc";

    public string GetSortSuffix(string column)
    {
        if (!string.Equals(SortBy, column, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return string.Equals(SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? " (asc)" : " (desc)";
    }

    public async Task OnGetAsync()
    {
        SearchPhone = string.IsNullOrWhiteSpace(SearchPhone) ? null : SearchPhone.Trim();
        SearchTelegramId = string.IsNullOrWhiteSpace(SearchTelegramId) ? null : SearchTelegramId.Trim();
        SearchDeepLink = string.IsNullOrWhiteSpace(SearchDeepLink) ? null : SearchDeepLink.Trim();
        SortBy = NormalizeSortBy(SortBy);
        SortDir = NormalizeSortDir(SortDir);

        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            StatusMessage = FlashMessage;
            StatusIsError = FlashIsError ?? false;
        }

        if (LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, HttpContext.RequestAborted);

        IQueryable<MiniAppUser> query = _db.Users;

        if (!string.IsNullOrWhiteSpace(SearchPhone))
            query = query.Where(x => x.Number != null && x.Number.Contains(SearchPhone));

        if (!string.IsNullOrWhiteSpace(SearchTelegramId))
        {
            if (!long.TryParse(SearchTelegramId, out var telegramUserId))
            {
                StatusMessage = "Telegram user id filter must be a valid integer.";
                StatusIsError = true;
                Users = new List<MiniAppUser>();
                return;
            }

            query = query.Where(x => x.TelegramUserId == telegramUserId);
        }

        if (!string.IsNullOrWhiteSpace(SearchDeepLink))
            query = query.Where(x => x.AcquisitionDeepLink != null && x.AcquisitionDeepLink.Contains(SearchDeepLink));

        query = ApplySort(query, SortBy, SortDir)
            .Take(200);

        Users = await query.ToListAsync();

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
            AcquisitionDeepLink = "admin-fake-user",
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
        return RedirectToPage(new
        {
            searchPhone = SearchPhone,
            searchTelegramId = SearchTelegramId,
            searchDeepLink = SearchDeepLink,
            sortBy = NormalizeSortBy(SortBy),
            sortDir = NormalizeSortDir(SortDir)
        });
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
            return RedirectToPage(new
            {
                searchPhone = SearchPhone,
                searchTelegramId = SearchTelegramId,
                searchDeepLink = SearchDeepLink,
                sortBy = NormalizeSortBy(SortBy),
                sortDir = NormalizeSortDir(SortDir)
            });

        _db.Users.Remove(u);
        await _db.SaveChangesAsync(ct);

        return RedirectToPage(new
        {
            searchPhone = SearchPhone,
            searchTelegramId = SearchTelegramId,
            searchDeepLink = SearchDeepLink,
            sortBy = NormalizeSortBy(SortBy),
            sortDir = NormalizeSortDir(SortDir)
        });
    }

    private static string NormalizeSortBy(string? value)
        => value?.Trim().ToLowerInvariant() switch
        {
            "id" => "id",
            "telegramuserid" => "telegramUserId",
            "number" => "number",
            "balance" => "balance",
            "createdat" => "createdAt",
            "createdatutc" => "createdAt",
            "acquisitiondeeplink" => "acquisitionDeepLink",
            "deeplink" => "acquisitionDeepLink",
            "lastseenat" => "lastSeenAt",
            "lastseenatutc" => "lastSeenAt",
            _ => "lastSeenAt"
        };

    private static string NormalizeSortDir(string? value)
        => string.Equals(value?.Trim(), "asc", StringComparison.OrdinalIgnoreCase)
            ? "asc"
            : "desc";

    private static IQueryable<MiniAppUser> ApplySort(IQueryable<MiniAppUser> query, string sortBy, string sortDir)
    {
        var isAsc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "id" => isAsc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id),
            "telegramUserId" => isAsc ? query.OrderBy(x => x.TelegramUserId) : query.OrderByDescending(x => x.TelegramUserId),
            "number" => isAsc ? query.OrderBy(x => x.Number) : query.OrderByDescending(x => x.Number),
            "acquisitionDeepLink" => isAsc ? query.OrderBy(x => x.AcquisitionDeepLink) : query.OrderByDescending(x => x.AcquisitionDeepLink),
            "balance" => isAsc ? query.OrderBy(x => x.Balance) : query.OrderByDescending(x => x.Balance),
            "createdAt" => isAsc ? query.OrderBy(x => x.CreatedAtUtc) : query.OrderByDescending(x => x.CreatedAtUtc),
            _ => isAsc ? query.OrderBy(x => x.LastSeenAtUtc) : query.OrderByDescending(x => x.LastSeenAtUtc)
        };
    }
}
