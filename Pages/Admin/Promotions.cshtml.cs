using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Localization;
using MiniApp.Features.Promotions;
using MiniApp.Features.Offers;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class PromotionsModel : LocalizedAdminPageModel
{
    public sealed record AdminPromotionRow(
        long Id,
        string Title,
        string TitleRu,
        string TitleUz,
        string Subtitle,
        string SubtitleRu,
        string SubtitleUz,
        string ButtonText,
        string ButtonTextRu,
        string ButtonTextUz,
        string ActionType,
        string? ActionValue,
        string CardStyle,
        string StylePreviewColor,
        string StylePreviewTextColor,
        int DisplayOrder,
        bool IsPublished,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record ActionTypeOption(string Value, string LabelKey, string FallbackLabel);
    public sealed record CardStyleOption(string Value, string Label);
    public sealed record DiscountedOfferOption(long Id, long DrawId, int NumberOfDiscountedTickets, decimal Cost);

    private readonly AppDbContext _db;

    public PromotionsModel(AppDbContext db, ILocalizationService localization)
        : base(localization)
    {
        _db = db;
    }

    public IReadOnlyList<AdminPromotionRow> Items { get; private set; } = Array.Empty<AdminPromotionRow>();
    public IReadOnlyList<ActionTypeOption> ActionTypeOptions { get; } =
    [
        new(PromotionsManagement.ActionTypeNone, "admin.promotions.actionType.none", "No action"),
        new(PromotionsManagement.ActionTypeAppSection, "admin.promotions.actionType.appSection", "Open app section"),
        new(PromotionsManagement.ActionTypeExternalUrl, "admin.promotions.actionType.externalUrl", "Open external link"),
        new(PromotionsManagement.ActionTypeDiscountedOffer, "admin.promotions.actionType.discountedOffer", "Open discounted offer")
    ];

    public IReadOnlyList<CardStyleOption> CardStyleOptions { get; } =
    [
        new(PromotionsManagement.CardStyleGold, "Gold (yellow)"),
        new(PromotionsManagement.CardStyleDark, "Dark (charcoal)"),
        new(PromotionsManagement.CardStyleRed, "Red"),
        new(PromotionsManagement.CardStyleWhite, "White")
    ];

    public IReadOnlyList<string> AppSectionOptions { get; } = PromotionsManagement.GetSupportedAppSections();
    public IReadOnlyList<DiscountedOfferOption> AvailableOffers { get; private set; } = Array.Empty<DiscountedOfferOption>();
    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        ApplyFlashMessage();
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string title,
        string titleRu,
        string titleUz,
        string subtitle,
        string subtitleRu,
        string subtitleUz,
        string buttonText,
        string buttonTextRu,
        string buttonTextUz,
        string cardStyle,
        int displayOrder,
        bool isPublished,
        string actionType,
        string? actionAppSection,
        string? actionExternalUrl,
        long? actionOfferId,
        CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var actionInputValue = GetActionInputValue(actionType, actionAppSection, actionExternalUrl, actionOfferId);
        if (!PromotionsManagement.TryNormalizeAction(actionType, actionInputValue, out var normalizedActionType, out var normalizedActionValue, out var actionValidationError))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync(actionValidationError, ct), isError: true);
            return RedirectToPage();
        }

        if (!await ValidateDiscountedOfferSelectionAsync(normalizedActionType, normalizedActionValue, ct))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync("invalid_discounted_offer", ct), isError: true);
            return RedirectToPage();
        }

        var now = DateTimeOffset.UtcNow;
        var promotion = new Promotion
        {
            Title = title?.Trim() ?? string.Empty,
            TitleRu = titleRu?.Trim() ?? string.Empty,
            TitleUz = titleUz?.Trim() ?? string.Empty,
            Subtitle = subtitle?.Trim() ?? string.Empty,
            SubtitleRu = subtitleRu?.Trim() ?? string.Empty,
            SubtitleUz = subtitleUz?.Trim() ?? string.Empty,
            ButtonText = buttonText?.Trim() ?? string.Empty,
            ButtonTextRu = buttonTextRu?.Trim() ?? string.Empty,
            ButtonTextUz = buttonTextUz?.Trim() ?? string.Empty,
            CardStyle = PromotionsManagement.NormalizeCardStyle(cardStyle),
            ActionType = normalizedActionType,
            ActionValue = normalizedActionValue,
            DisplayOrder = Math.Max(0, displayOrder),
            IsPublished = isPublished,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.Promotions.Add(promotion);

        try
        {
            await _db.SaveChangesAsync(ct);
            var template = await GetTextAsync("admin.promotions.flash.created", "Promotion #{0} created.", ct);
            await SetFlashAsync(string.Format(template, promotion.Id), isError: false);
        }
        catch
        {
            await SetFlashAsync(await GetTextAsync("admin.promotions.flash.saveFailed", "Promotion save failed.", ct), isError: true);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        long id,
        string title,
        string titleRu,
        string titleUz,
        string subtitle,
        string subtitleRu,
        string subtitleUz,
        string buttonText,
        string buttonTextRu,
        string buttonTextUz,
        string cardStyle,
        int displayOrder,
        bool isPublished,
        string actionType,
        string? actionAppSection,
        string? actionExternalUrl,
        long? actionOfferId,
        CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var actionInputValue = GetActionInputValue(actionType, actionAppSection, actionExternalUrl, actionOfferId);
        if (!PromotionsManagement.TryNormalizeAction(actionType, actionInputValue, out var normalizedActionType, out var normalizedActionValue, out var actionValidationError))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync(actionValidationError, ct), isError: true);
            return RedirectToPage();
        }

        if (!await ValidateDiscountedOfferSelectionAsync(normalizedActionType, normalizedActionValue, ct))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync("invalid_discounted_offer", ct), isError: true);
            return RedirectToPage();
        }

        var promotion = await _db.Promotions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (promotion is null)
        {
            var notFound = await GetTextAsync("admin.promotions.flash.notFound", "Promotion was not found.", ct);
            await SetFlashAsync(notFound, isError: true);
            return RedirectToPage();
        }

        promotion.Title = title?.Trim() ?? string.Empty;
        promotion.TitleRu = titleRu?.Trim() ?? string.Empty;
        promotion.TitleUz = titleUz?.Trim() ?? string.Empty;
        promotion.Subtitle = subtitle?.Trim() ?? string.Empty;
        promotion.SubtitleRu = subtitleRu?.Trim() ?? string.Empty;
        promotion.SubtitleUz = subtitleUz?.Trim() ?? string.Empty;
        promotion.ButtonText = buttonText?.Trim() ?? string.Empty;
        promotion.ButtonTextRu = buttonTextRu?.Trim() ?? string.Empty;
        promotion.ButtonTextUz = buttonTextUz?.Trim() ?? string.Empty;
        promotion.CardStyle = PromotionsManagement.NormalizeCardStyle(cardStyle);
        promotion.DisplayOrder = Math.Max(0, displayOrder);
        promotion.IsPublished = isPublished;
        promotion.ActionType = normalizedActionType;
        promotion.ActionValue = normalizedActionValue;
        promotion.UpdatedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            await SetFlashAsync(await GetTextAsync("admin.promotions.flash.saveFailed", "Promotion save failed.", ct), isError: true);
            return RedirectToPage();
        }

        var template = await GetTextAsync("admin.promotions.flash.updated", "Promotion #{0} updated.", ct);
        await SetFlashAsync(string.Format(template, promotion.Id), isError: false);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var promotion = await _db.Promotions.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (promotion is null)
        {
            var notFound = await GetTextAsync("admin.promotions.flash.notFound", "Promotion was not found.", ct);
            await SetFlashAsync(notFound, isError: true);
            return RedirectToPage();
        }

        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync(ct);

        var template = await GetTextAsync("admin.promotions.flash.deleted", "Promotion #{0} deleted.", ct);
        await SetFlashAsync(string.Format(template, id), isError: false);
        return RedirectToPage();
    }

    private void ApplyFlashMessage()
    {
        if (string.IsNullOrWhiteSpace(FlashMessage))
            return;

        StatusMessage = FlashMessage;
        StatusIsError = FlashIsError ?? false;
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        AvailableOffers = await LoadAvailableOffersAsync(ct);

        Items = await _db.Promotions
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminPromotionRow(
                x.Id,
                x.Title,
                x.TitleRu,
                x.TitleUz,
                x.Subtitle,
                x.SubtitleRu,
                x.SubtitleUz,
                x.ButtonText,
                x.ButtonTextRu,
                x.ButtonTextUz,
                PromotionsManagement.NormalizeStoredActionType(x.ActionType),
                PromotionsManagement.NormalizeStoredActionValue(x.ActionType, x.ActionValue),
                PromotionsManagement.NormalizeCardStyle(x.CardStyle),
                GetStylePreviewColor(x.CardStyle),
                GetStylePreviewTextColor(x.CardStyle),
                x.DisplayOrder,
                x.IsPublished,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    private async Task SetFlashAsync(string message, bool isError)
    {
        FlashMessage = message;
        FlashIsError = isError;
        await Task.CompletedTask;
    }

    private async Task<string> GetActionValidationMessageAsync(string validationErrorCode, CancellationToken ct)
    {
        return validationErrorCode switch
        {
            "invalid_type" => await GetTextAsync("admin.promotions.flash.actionTypeInvalid", "Select a valid promotion action.", ct),
            "missing_value" => await GetTextAsync("admin.promotions.flash.actionValueRequired", "Enter the action target for this promotion.", ct),
            "invalid_app_section" => await GetTextAsync("admin.promotions.flash.actionAppSectionInvalid", "Choose a valid app section target.", ct),
            "invalid_discounted_offer" => await GetTextAsync("admin.promotions.flash.actionDiscountedOfferInvalid", "Choose a valid active discounted offer.", ct),
            "invalid_external_url" => await GetTextAsync("admin.promotions.flash.actionUrlInvalid", "Enter a valid absolute HTTPS URL.", ct),
            _ => await GetTextAsync("admin.promotions.flash.actionTypeInvalid", "Select a valid promotion action.", ct)
        };
    }

    private static string? GetActionInputValue(string? actionType, string? actionAppSection, string? actionExternalUrl, long? actionOfferId)
    {
        var normalizedActionType = PromotionsManagement.NormalizeStoredActionType(actionType);
        return normalizedActionType switch
        {
            PromotionsManagement.ActionTypeAppSection => actionAppSection,
            PromotionsManagement.ActionTypeExternalUrl => actionExternalUrl,
            PromotionsManagement.ActionTypeDiscountedOffer => actionOfferId?.ToString(),
            _ => null
        };
    }

    private static string GetStylePreviewColor(string? cardStyle) =>
        PromotionsManagement.NormalizeCardStyle(cardStyle) switch
        {
            PromotionsManagement.CardStyleDark => "#1C2140",
            PromotionsManagement.CardStyleRed => "#D42B3A",
            PromotionsManagement.CardStyleWhite => "#FFFFFF",
            _ => "#FFB929"
        };

    private static string GetStylePreviewTextColor(string? cardStyle) =>
        PromotionsManagement.NormalizeCardStyle(cardStyle) switch
        {
            PromotionsManagement.CardStyleGold => "#1C1C2E",
            PromotionsManagement.CardStyleWhite => "#0F0F12",
            _ => "#FFFFFF"
        };

    private async Task<bool> ValidateDiscountedOfferSelectionAsync(string normalizedActionType, string? normalizedActionValue, CancellationToken ct)
    {
        if (!string.Equals(normalizedActionType, PromotionsManagement.ActionTypeDiscountedOffer, StringComparison.Ordinal))
            return true;

        if (!long.TryParse(normalizedActionValue, out var offerId) || offerId <= 0)
            return false;

        var nowUtc = DateTimeOffset.UtcNow;
        var offer = await _db.DiscountedTicketOffers
            .AsNoTracking()
            .Include(x => x.Draw)
            .SingleOrDefaultAsync(x => x.Id == offerId, ct);

        return offer is not null && DiscountedTicketOfferManagement.IsAvailable(offer, offer.Draw, nowUtc);
    }

    private async Task<IReadOnlyList<DiscountedOfferOption>> LoadAvailableOffersAsync(CancellationToken ct)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var offers = await _db.DiscountedTicketOffers
            .AsNoTracking()
            .Include(x => x.Draw)
            .Where(x => x.IsActive && x.Draw.State == DrawState.Active)
            .OrderByDescending(x => x.DrawId)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        return offers
            .Where(x => DiscountedTicketOfferManagement.IsAvailable(x, x.Draw, nowUtc))
            .Select(x => new DiscountedOfferOption(x.Id, x.DrawId, x.NumberOfDiscountedTickets, x.Cost))
            .ToArray();
    }
}
