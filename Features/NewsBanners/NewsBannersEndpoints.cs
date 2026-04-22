using Microsoft.EntityFrameworkCore;
using MiniApp.Data;

namespace MiniApp.Features.NewsBanners;

public static class NewsBannersEndpoints
{
    public static IEndpointRouteBuilder MapNewsBannersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/news-banners", async (AppDbContext db, CancellationToken ct) =>
        {
            var banners = await db.NewsBanners
                .AsNoTracking()
                .Where(x => x.IsPublished)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Select(x => NewsBannerManagement.ToDto(x))
                .ToListAsync(ct);

            return Results.Ok(new NewsBannersListResult(true, banners));
        });

        return endpoints;
    }
}

