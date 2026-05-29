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
        string Subtitle,
        string ButtonText,
        string ActionType,
        string? ActionValue,
        string BackgroundColor,
        int DisplayOrder,
        bool IsPublished,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record ActionTypeOption(string Value, string LabelKey, string FallbackLabel);
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
        string subtitle,
        string buttonText,
        string backgroundColor,
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
            Subtitle = subtitle?.Trim() ?? string.Empty,
            ButtonText = buttonText?.Trim() ?? string.Empty,
            BackgroundColor = (backgroundColor?.Trim() ?? string.Empty).Length > 0 ? backgroundColor!.Trim() : "#FFB929",
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
        string subtitle,
        string buttonText,
        string backgroundColor,
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
        promotion.Subtitle = subtitle?.Trim() ?? string.Empty;
        promotion.ButtonText = buttonText?.Trim() ?? string.Empty;
        promotion.BackgroundColor = (backgroundColor?.Trim() ?? string.Empty).Length > 0 ? backgroundColor!.Trim() : "#FFB929";
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
                x.Subtitle,
                x.ButtonText,
                PromotionsManagement.NormalizeStoredActionType(x.ActionType),
                PromotionsManagement.NormalizeStoredActionValue(x.ActionType, x.ActionValue),
                x.BackgroundColor,
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
