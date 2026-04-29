using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;
using MiniApp.Features.Localization;
using MiniApp.Features.Offers;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.NewsBanners;

public static class NewsBannersEndpoints
{
    public static IEndpointRouteBuilder MapNewsBannersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/news-banners", async (AppDbContext db, CancellationToken ct) =>
        {
            var bannerDtos = await BuildBannerDtosAsync(db, locale: null, ct);
            return Results.Ok(new NewsBannersListResult(true, bannerDtos));
        });

        endpoints.MapPost("/api/news-banners", async (
            NewsBannersRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
            ILocalizationService localization,
            CancellationToken ct) =>
        {
            var authResult = await TryResolveTelegramUserIdAsync(req.InitData ?? string.Empty, http, config, env, db, ct);
            if (authResult.ErrorResult is not null)
                return authResult.ErrorResult;

            var telegramUserId = authResult.TelegramUserId!.Value;
            var user = await users.TouchUserAsync(telegramUserId, ct);
            var locale = localization.NormalizeLocale(user.PreferredLanguage ?? req.Locale);
            var bannerDtos = await BuildBannerDtosAsync(db, locale, ct);
            return Results.Ok(new NewsBannersListResult(true, bannerDtos));
        });

        return endpoints;
    }

    private static async Task<List<NewsBannerDto>> BuildBannerDtosAsync(AppDbContext db, string? locale, CancellationToken ct)
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

        return banners
            .Select(x =>
            {
                DiscountedTicketOfferDto? offer = null;
                if (string.Equals(NewsBannerManagement.NormalizeStoredActionType(x.ActionType), NewsBannerManagement.ActionTypeDiscountedOffer, StringComparison.Ordinal)
                    && long.TryParse(x.ActionValue, out var offerId)
                    && offersById.TryGetValue(offerId, out var foundOffer))
                {
                    offer = foundOffer;
                }

                return NewsBannerManagement.ToDto(x, locale, offer);
            })
            .ToList();
    }

    private static async Task<(long? TelegramUserId, IResult? ErrorResult)> TryResolveTelegramUserIdAsync(
        string initData,
        HttpContext http,
        IConfiguration config,
        IWebHostEnvironment env,
        AppDbContext db,
        CancellationToken ct)
    {
        if (LocalDebugMode.TryGetDebugTelegramUserId(http, config, env, out var localDebugUserId))
        {
            await LocalDebugSeed.EnsureSeededAsync(db, localDebugUserId, ct);
            return (localDebugUserId, null);
        }

        var botToken = config["BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            return (null, Results.Problem("BotToken is not configured.", statusCode: 500));

        if (!TelegramInitDataValidator.TryValidateInitData(initData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out var error))
        {
            if (env.IsDevelopment())
                return (null, Results.Json(new { ok = false, error }, statusCode: StatusCodes.Status401Unauthorized));
            return (null, Results.Unauthorized());
        }

        return (tgUser!.Id, null);
    }
}

