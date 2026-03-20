using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Draws;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class DrawsModel : PageModel
{
    private readonly AppDbContext _db;

    public DrawsModel(AppDbContext db)
    {
        _db = db;
    }

    public DrawDto? LatestDraw { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostStartAsync(CancellationToken ct)
    {
        var draw = new Draw
        {
            Numbers = DrawsEndpoints.GenerateDrawNumbers(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _db.Draws.Add(draw);
        await _db.SaveChangesAsync(ct);

        StatusMessage = $"Started draw #{draw.Id} ({draw.Numbers}).";
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var d = await _db.Draws
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DrawDto(x.Id, x.Numbers, x.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);

        LatestDraw = d;
    }
}

