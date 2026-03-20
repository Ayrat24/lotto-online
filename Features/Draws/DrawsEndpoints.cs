using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using MiniApp.Admin;
using MiniApp.Data;

namespace MiniApp.Features.Draws;

public static class DrawsEndpoints
{
    public static IEndpointRouteBuilder MapDrawsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Admin: start the next draw (generate 6 numbers) with the next sequential id.
        endpoints.MapPost("/api/admin/draws/start", [Authorize(Policy = AdminAuth.PolicyName)] async (
            AppDbContext db,
            IHubContext<DrawsHub> hub,
            CancellationToken ct) =>
        {
            var nextId = (await db.Draws.MaxAsync(x => (long?)x.Id, ct) ?? 0) + 1;

            var draw = new Draw
            {
                Id = nextId,
                Numbers = GenerateDrawNumbers(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            db.Draws.Add(draw);
            await db.SaveChangesAsync(ct);

            var dto = new DrawDto(draw.Id, draw.Numbers, draw.CreatedAtUtc);

            // Push event to all connected clients.
            await hub.Clients.All.SendAsync("draw_created", new { draw = dto }, ct);

            return Results.Ok(new { ok = true, draw = dto });
        });

        // Public: list recent draws
        endpoints.MapGet("/api/draws", async (AppDbContext db, CancellationToken ct) =>
        {
            var draws = await db.Draws
                .OrderByDescending(d => d.Id)
                .Take(100)
                .Select(d => new DrawDto(d.Id, d.Numbers, d.CreatedAtUtc))
                .AsNoTracking()
                .ToListAsync(ct);

            return Results.Ok(new { ok = true, draws });
        });

        return endpoints;
    }

    internal static string GenerateDrawNumbers()
    {
        var set = new HashSet<int>();
        while (set.Count < 6)
            set.Add(Random.Shared.Next(1, 50));

        var arr = set.ToArray();
        Array.Sort(arr);
        return string.Join(',', arr);
    }
}
