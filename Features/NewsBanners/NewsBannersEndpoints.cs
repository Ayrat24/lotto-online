using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.Features.Draws;
using MiniApp.Features.Offers;

namespace MiniApp.Features.NewsBanners;

public static class NewsBannersEndpoints
{
    public static IEndpointRouteBuilder MapNewsBannersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/news-banners", async (AppDbContext db, CancellationToken ct) =>
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var activeDraws = await db.Draws
                .AsNoTracking()
                .Where(x => x.State == DrawState.Active)
                .ToListAsync(ct);

            var activeDrawsById = activeDraws
                .Where(x => DrawManagement.CanPurchase(x, nowUtc))
                .ToDictionary(x => x.Id);

            var offers = activeDrawsById.Count == 0
                ? Array.Empty<DiscountedTicketOfferDto>()
                : (await db.DiscountedTicketOffers
                    .AsNoTracking()
                    .Where(x => x.IsActive && activeDrawsById.Keys.Contains(x.DrawId))
                    .OrderByDescending(x => x.UpdatedAtUtc)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync(ct))
                    .Where(x => activeDrawsById.TryGetValue(x.DrawId, out var draw)
                        && DiscountedTicketOfferManagement.IsAvailable(x, draw, nowUtc))
                    .Select(DiscountedTicketOfferManagement.ToDto)
                    .ToArray();

            var offersById = offers.ToDictionary(x => x.Id);

            var banners = await db.NewsBanners
                .AsNoTracking()
                .Where(x => x.IsPublished)
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.CreatedAtUtc)
                .ToListAsync(ct);

            var bannerDtos = banners
                .Select(x =>
                {
                    DiscountedTicketOfferDto? offer = null;
                    if (string.Equals(NewsBannerManagement.NormalizeStoredActionType(x.ActionType), NewsBannerManagement.ActionTypeDiscountedOffer, StringComparison.Ordinal)
                        && long.TryParse(x.ActionValue, out var offerId)
                        && offersById.TryGetValue(offerId, out var foundOffer))
                    {
                        offer = foundOffer;
                    }

                    return NewsBannerManagement.ToDto(x, offer);
                })
                .ToList();

            return Results.Ok(new NewsBannersListResult(true, bannerDtos));
        });

        return endpoints;
    }
}

