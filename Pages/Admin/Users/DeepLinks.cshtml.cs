using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public List<DeepLinkCountView> Rows { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? SearchPhone { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTelegramId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchDeepLink { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortDir { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PageNumber { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var grouped = await _db.Users
            .AsNoTracking()
            .GroupBy(x => x.AcquisitionDeepLink)
            .Select(x => new
            {
                DeepLink = x.Key,
                UserCount = x.Count()
            })
            .OrderByDescending(x => x.UserCount)
            .ThenBy(x => x.DeepLink)
            .ToListAsync(ct);

        Rows = grouped
            .Select(x => new DeepLinkCountView(
                string.IsNullOrWhiteSpace(x.DeepLink) ? "(none)" : x.DeepLink!,
                x.UserCount))
            .ToList();
    }

    public sealed record DeepLinkCountView(string DeepLink, int UserCount);
}

