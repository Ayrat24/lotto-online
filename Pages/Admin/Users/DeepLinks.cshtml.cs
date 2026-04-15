using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;

namespace MiniApp.Pages.Admin.Users;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class DeepLinksModel : PageModel
{
    private readonly AppDbContext _db;

    public DeepLinksModel(AppDbContext db)
    {
        _db = db;
    }

    public List<DeepLinkGroupView> Groups { get; private set; } = new();

    public async Task OnGetAsync(CancellationToken ct)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DeepLinkUserView(
                x.Id,
                x.TelegramUserId,
                x.Number,
                x.Balance,
                x.CreatedAtUtc,
                x.LastSeenAtUtc,
                x.IsFake,
                x.AcquisitionDeepLink))
            .ToListAsync(ct);

        Groups = users
            .GroupBy(x => string.IsNullOrWhiteSpace(x.AcquisitionDeepLink) ? "(none)" : x.AcquisitionDeepLink!, StringComparer.Ordinal)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => new DeepLinkGroupView(x.Key, x.ToList()))
            .ToList();
    }

    public sealed record DeepLinkGroupView(string DeepLink, List<DeepLinkUserView> Users)
    {
        public int UserCount => Users.Count;
    }

    public sealed record DeepLinkUserView(
        long Id,
        long TelegramUserId,
        string? Number,
        decimal Balance,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset LastSeenAtUtc,
        bool IsFake,
        string? AcquisitionDeepLink);
}

