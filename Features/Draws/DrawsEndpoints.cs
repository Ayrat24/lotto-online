using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;

namespace MiniApp.Features.Draws;

public static class DrawsEndpoints
{
    public static IEndpointRouteBuilder MapDrawsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Admin: create the next draw instance. If there is no active draw, the new instance becomes active.
        endpoints.MapPost("/api/admin/draws/start", [Authorize(Policy = AdminAuth.PolicyName)] async (
            CreateDrawRequest? req,
            AppDbContext db,
            CancellationToken ct) =>
        {
            try
            {
                var request = req ?? new CreateDrawRequest(0m, 0m, 0m);
                var draw = await DrawManagement.CreateDrawAsync(
                    db,
                    request.PrizePoolMatch3,
                    request.PrizePoolMatch4,
                    request.PrizePoolMatch5,
                    ct);
                var dto = DrawManagement.ToDto(draw);
                return Results.Ok(new { ok = true, draw = dto });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { ok = false, error = ex.Message });
            }

        });

        endpoints.MapPost("/api/admin/draws/{id:long}/update", [Authorize(Policy = AdminAuth.PolicyName)] async (
            long id,
            UpdateDrawRequest req,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var draw = await db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (draw is null)
                return Results.NotFound(new { ok = false, error = $"Draw #{id} was not found." });

            if (!DrawManagement.TryParseEditableState(req.State, out var state))
                return Results.BadRequest(new { ok = false, error = "State must be active or upcoming." });

            try
            {
                await DrawManagement.UpdateDrawAsync(
                    db,
                    draw,
                    req.PrizePoolMatch3,
                    req.PrizePoolMatch4,
                    req.PrizePoolMatch5,
                    state,
                    ct);
                return Results.Ok(new { ok = true, draw = DrawManagement.ToDto(draw) });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { ok = false, error = ex.Message });
            }
        });

        endpoints.MapPost("/api/admin/draws/{id:long}/execute", [Authorize(Policy = AdminAuth.PolicyName)] async (
            long id,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var draw = await db.Draws.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (draw is null)
                return Results.NotFound(new { ok = false, error = $"Draw #{id} was not found." });

            try
            {
                await DrawManagement.ExecuteDrawAsync(db, draw, null, ct);
                return Results.Ok(new { ok = true, draw = DrawManagement.ToDto(draw) });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { ok = false, error = ex.Message });
            }
        });

        // Public: list recent draws.
        endpoints.MapGet("/api/draws", async (AppDbContext db, CancellationToken ct) =>
        {
            var draws = await db.Draws
                .AsNoTracking()
                .OrderByDescending(d => d.Id)
                .Take(100)
                .ToListAsync(ct);

            var drawDtos = draws
                .Select(DrawManagement.ToDto)
                .ToArray();

            return Results.Ok(new { ok = true, draws = drawDtos });
        });

        return endpoints;
    }
}
