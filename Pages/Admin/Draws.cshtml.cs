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
    private readonly AppDbContext db;

    public DrawsModel(AppDbContext db)
    {
        this.db = db;
    }

    public IReadOnlyList<DrawDto> Draws { get; private set; } = Array.Empty<DrawDto>();
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostStartAsync(CancellationToken ct)
    {
        var nextId = (await db.Draws.MaxAsync(x => (long?)x.Id, ct) ?? 0) + 1;

        var draw = new Draw
        {
            Id = nextId,
            Numbers = DrawsEndpoints.GenerateDrawNumbers(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Draws.Add(draw);
        await db.SaveChangesAsync(ct);

        StatusMessage = $"Started draw #{draw.Id} ({draw.Numbers}).";
        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Draws = await db.Draws
            .OrderByDescending(x => x.Id)
            .Take(200)
            .Select(x => new DrawDto(x.Id, x.Numbers, x.CreatedAtUtc))
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
