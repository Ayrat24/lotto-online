using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Localization;
using MiniApp.Features.NewsBanners;
using MiniApp.Features.Offers;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class NewsBannersModel : LocalizedAdminPageModel
{
    public sealed record AdminNewsBannerRow(
        long Id,
        string ImagePath,
        string? ImagePathEn,
        string? ImagePathRu,
        string? ImagePathUz,
        string ActionType,
        string? ActionValue,
        int DisplayOrder,
        bool IsPublished,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record ActionTypeOption(string Value, string LabelKey, string FallbackLabel);
    public sealed record DiscountedOfferOption(long Id, long DrawId, int NumberOfDiscountedTickets, decimal Cost);
    public sealed record BannerImageSlot(string Code, string FieldName, string LabelKey, string FallbackLabel, bool IsRequiredOnCreate);

    private sealed record BannerImagePaths(string? DefaultImagePath, string? EnglishImagePath, string? RussianImagePath, string? UzbekImagePath)
    {
        public IReadOnlyList<string> GetAllPaths()
            => new[] { DefaultImagePath, EnglishImagePath, RussianImagePath, UzbekImagePath }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
    }

    private sealed record BannerImageUploadFailure(BannerImageSlot Slot, NewsBannerImageSaveResult SaveResult);

    private sealed record BannerImageSlotUploadResult(bool Ok, string? PublicImagePath, BannerImageUploadFailure? Failure);

    private sealed record BannerImageUploadSetResult(bool Ok, BannerImagePaths? Paths, BannerImageUploadFailure? Failure);

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public NewsBannersModel(AppDbContext db, IWebHostEnvironment env, ILocalizationService localization)
        : base(localization)
    {
        _db = db;
        _env = env;
    }

    public IReadOnlyList<AdminNewsBannerRow> Items { get; private set; } = Array.Empty<AdminNewsBannerRow>();
    public IReadOnlyList<ActionTypeOption> ActionTypeOptions { get; } =
    [
        new(NewsBannerManagement.ActionTypeNone, "admin.newsBanners.actionType.none", "No action"),
        new(NewsBannerManagement.ActionTypeAppSection, "admin.newsBanners.actionType.appSection", "Open app section"),
        new(NewsBannerManagement.ActionTypeExternalUrl, "admin.newsBanners.actionType.externalUrl", "Open external link"),
        new(NewsBannerManagement.ActionTypeDiscountedOffer, "admin.newsBanners.actionType.discountedOffer", "Open discounted offer")
    ];

    public IReadOnlyList<string> AppSectionOptions { get; } = NewsBannerManagement.GetSupportedAppSections();
    public IReadOnlyList<DiscountedOfferOption> AvailableOffers { get; private set; } = Array.Empty<DiscountedOfferOption>();
    public IReadOnlyList<BannerImageSlot> ImageSlots { get; } =
    [
        new("default", "imageFile", "admin.newsBanners.create.image", "Default / fallback image", true),
        new("en", "imageFileEn", "admin.newsBanners.create.imageEn", "English image", false),
        new("ru", "imageFileRu", "admin.newsBanners.create.imageRu", "Russian image", false),
        new("uz", "imageFileUz", "admin.newsBanners.create.imageUz", "Uzbek image", false)
    ];
    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }
    public int RequiredWidth => NewsBannerManagement.RequiredImageWidth;
    public int RequiredHeight => NewsBannerManagement.RequiredImageHeight;
    public int MaxUploadMegabytes => (int)Math.Ceiling(NewsBannerManagement.MaxUploadBytes / 1024d / 1024d);

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

    public async Task<IActionResult> OnPostCreateAsync(IFormFile? imageFile, IFormFile? imageFileEn, IFormFile? imageFileRu, IFormFile? imageFileUz, int displayOrder, bool isPublished, string actionType, string? actionAppSection, string? actionExternalUrl, long? actionOfferId, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var actionInputValue = GetActionInputValue(actionType, actionAppSection, actionExternalUrl, actionOfferId);
        if (!NewsBannerManagement.TryNormalizeAction(actionType, actionInputValue, out var normalizedActionType, out var normalizedActionValue, out var actionValidationError))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync(actionValidationError, ct), isError: true);
            return RedirectToPage();
        }

        if (!await ValidateDiscountedOfferSelectionAsync(normalizedActionType, normalizedActionValue, ct))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync("invalid_discounted_offer", ct), isError: true);
            return RedirectToPage();
        }

        var uploadResult = await SaveBannerImagesAsync(imageFile, imageFileEn, imageFileRu, imageFileUz, requireDefaultImage: true, ct);
        if (!uploadResult.Ok || uploadResult.Paths is null || string.IsNullOrWhiteSpace(uploadResult.Paths.DefaultImagePath))
        {
            var failure = uploadResult.Failure;
            await SetFlashAsync(await GetUploadErrorMessageAsync(
                failure?.SaveResult ?? new NewsBannerImageSaveResult(false, null, NewsBannerImageError.MissingFile),
                failure?.Slot,
                ct), isError: true);
            return RedirectToPage();
        }

        var imagePaths = uploadResult.Paths;

        var now = DateTimeOffset.UtcNow;
        var banner = new NewsBanner
        {
            ImagePath = imagePaths.DefaultImagePath!,
            ImagePathEn = imagePaths.EnglishImagePath,
            ImagePathRu = imagePaths.RussianImagePath,
            ImagePathUz = imagePaths.UzbekImagePath,
            ActionType = normalizedActionType,
            ActionValue = normalizedActionValue,
            DisplayOrder = Math.Max(0, displayOrder),
            IsPublished = isPublished,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.NewsBanners.Add(banner);

        try
        {
            await _db.SaveChangesAsync(ct);
            var template = await GetTextAsync("admin.newsBanners.flash.created", "Banner #{0} created.", ct);
            await SetFlashAsync(string.Format(template, banner.Id), isError: false);
        }
        catch
        {
            NewsBannerManagement.DeleteImagesIfExists(_env, imagePaths.GetAllPaths());
            await SetFlashAsync(await GetTextAsync("admin.newsBanners.flash.uploadFailed", "Banner upload failed.", ct), isError: true);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(long id, int displayOrder, bool isPublished, string actionType, string? actionAppSection, string? actionExternalUrl, long? actionOfferId, IFormFile? imageFile, IFormFile? imageFileEn, IFormFile? imageFileRu, IFormFile? imageFileUz, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var actionInputValue = GetActionInputValue(actionType, actionAppSection, actionExternalUrl, actionOfferId);
        if (!NewsBannerManagement.TryNormalizeAction(actionType, actionInputValue, out var normalizedActionType, out var normalizedActionValue, out var actionValidationError))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync(actionValidationError, ct), isError: true);
            return RedirectToPage();
        }

        if (!await ValidateDiscountedOfferSelectionAsync(normalizedActionType, normalizedActionValue, ct))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync("invalid_discounted_offer", ct), isError: true);
            return RedirectToPage();
        }

        var banner = await _db.NewsBanners.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (banner is null)
        {
            var notFound = await GetTextAsync("admin.newsBanners.flash.notFound", "Banner was not found.", ct);
            await SetFlashAsync(notFound, isError: true);
            return RedirectToPage();
        }

        var uploadResult = await SaveBannerImagesAsync(imageFile, imageFileEn, imageFileRu, imageFileUz, requireDefaultImage: false, ct);
        if (!uploadResult.Ok)
        {
            await SetFlashAsync(await GetUploadErrorMessageAsync(uploadResult.Failure!.SaveResult, uploadResult.Failure.Slot, ct), isError: true);
            return RedirectToPage();
        }

        var replacementPaths = uploadResult.Paths!;
        var previousImagePaths = NewsBannerManagement.GetAllImagePaths(banner).ToHashSet(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(replacementPaths.DefaultImagePath))
            banner.ImagePath = replacementPaths.DefaultImagePath!;

        if (!string.IsNullOrWhiteSpace(replacementPaths.EnglishImagePath))
            banner.ImagePathEn = replacementPaths.EnglishImagePath;

        if (!string.IsNullOrWhiteSpace(replacementPaths.RussianImagePath))
            banner.ImagePathRu = replacementPaths.RussianImagePath;

        if (!string.IsNullOrWhiteSpace(replacementPaths.UzbekImagePath))
            banner.ImagePathUz = replacementPaths.UzbekImagePath;

        banner.DisplayOrder = Math.Max(0, displayOrder);
        banner.IsPublished = isPublished;
        banner.ActionType = normalizedActionType;
        banner.ActionValue = normalizedActionValue;
        banner.UpdatedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            NewsBannerManagement.DeleteImagesIfExists(_env, replacementPaths.GetAllPaths());
            await SetFlashAsync(await GetTextAsync("admin.newsBanners.flash.uploadFailed", "Banner upload failed.", ct), isError: true);
            return RedirectToPage();
        }

        var activeImagePaths = NewsBannerManagement.GetAllImagePaths(banner).ToHashSet(StringComparer.Ordinal);
        var obsoleteImagePaths = previousImagePaths.Where(x => !activeImagePaths.Contains(x)).ToArray();
        NewsBannerManagement.DeleteImagesIfExists(_env, obsoleteImagePaths);

        var template = await GetTextAsync("admin.newsBanners.flash.updated", "Banner #{0} updated.", ct);
        await SetFlashAsync(string.Format(template, banner.Id), isError: false);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var banner = await _db.NewsBanners.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (banner is null)
        {
            var notFound = await GetTextAsync("admin.newsBanners.flash.notFound", "Banner was not found.", ct);
            await SetFlashAsync(notFound, isError: true);
            return RedirectToPage();
        }

        var imagePaths = NewsBannerManagement.GetAllImagePaths(banner);
        _db.NewsBanners.Remove(banner);
        await _db.SaveChangesAsync(ct);
        NewsBannerManagement.DeleteImagesIfExists(_env, imagePaths);

        var template = await GetTextAsync("admin.newsBanners.flash.deleted", "Banner #{0} deleted.", ct);
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

        Items = await _db.NewsBanners
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminNewsBannerRow(
                x.Id,
                x.ImagePath,
                x.ImagePathEn,
                x.ImagePathRu,
                x.ImagePathUz,
                NewsBannerManagement.NormalizeStoredActionType(x.ActionType),
                NewsBannerManagement.NormalizeStoredActionValue(x.ActionType, x.ActionValue),
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

    public string? GetImagePath(AdminNewsBannerRow row, string code)
    {
        ArgumentNullException.ThrowIfNull(row);

        return code switch
        {
            "en" => row.ImagePathEn,
            "ru" => row.ImagePathRu,
            "uz" => row.ImagePathUz,
            _ => row.ImagePath
        };
    }

    private async Task<string> GetUploadErrorMessageAsync(NewsBannerImageSaveResult result, BannerImageSlot? slot, CancellationToken ct)
    {
        var message = result.Error switch
        {
            NewsBannerImageError.MissingFile => await GetTextAsync("admin.newsBanners.flash.uploadMissing", "Select a JPEG image to upload.", ct),
            NewsBannerImageError.InvalidType => await GetTextAsync("admin.newsBanners.flash.uploadInvalidType", "Only .jpg or .jpeg images are allowed.", ct),
            NewsBannerImageError.FileTooLarge => string.Format(
                await GetTextAsync("admin.newsBanners.flash.uploadFileTooLarge", "Banner image is too large. Keep it under {0} MB.", ct),
                MaxUploadMegabytes),
            NewsBannerImageError.InvalidJpeg => await GetTextAsync("admin.newsBanners.flash.uploadInvalidJpeg", "The uploaded file is not a valid JPEG image.", ct),
            NewsBannerImageError.InvalidDimensions => string.Format(
                await GetTextAsync("admin.newsBanners.flash.uploadInvalidDimensions", "Banner must be exactly {0}×{1}px. Uploaded image is {2}×{3}px.", ct),
                RequiredWidth,
                RequiredHeight,
                result.ActualWidth,
                result.ActualHeight),
            _ => await GetTextAsync("admin.newsBanners.flash.uploadFailed", "Banner upload failed.", ct)
        };

        if (slot is null)
            return message;

        var label = await GetTextAsync(slot.LabelKey, slot.FallbackLabel, ct);
        var prefixTemplate = await GetTextAsync("admin.newsBanners.flash.uploadPrefixed", "{0}: {1}", ct);
        return string.Format(prefixTemplate, label, message);
    }

    private async Task<string> GetActionValidationMessageAsync(string validationErrorCode, CancellationToken ct)
    {
        return validationErrorCode switch
        {
            "invalid_type" => await GetTextAsync("admin.newsBanners.flash.actionTypeInvalid", "Select a valid banner action.", ct),
            "missing_value" => await GetTextAsync("admin.newsBanners.flash.actionValueRequired", "Enter the action target for this banner.", ct),
            "invalid_app_section" => await GetTextAsync("admin.newsBanners.flash.actionAppSectionInvalid", "Choose a valid app section target.", ct),
            "invalid_discounted_offer" => await GetTextAsync("admin.newsBanners.flash.actionDiscountedOfferInvalid", "Choose a valid active discounted offer.", ct),
            "invalid_external_url" => await GetTextAsync("admin.newsBanners.flash.actionUrlInvalid", "Enter a valid absolute HTTPS URL.", ct),
            _ => await GetTextAsync("admin.newsBanners.flash.actionTypeInvalid", "Select a valid banner action.", ct)
        };
    }

    private static string? GetActionInputValue(string? actionType, string? actionAppSection, string? actionExternalUrl, long? actionOfferId)
    {
        var normalizedActionType = NewsBannerManagement.NormalizeStoredActionType(actionType);
        return normalizedActionType switch
        {
            NewsBannerManagement.ActionTypeAppSection => actionAppSection,
            NewsBannerManagement.ActionTypeExternalUrl => actionExternalUrl,
            NewsBannerManagement.ActionTypeDiscountedOffer => actionOfferId?.ToString(),
            _ => null
        };
    }

    private async Task<bool> ValidateDiscountedOfferSelectionAsync(string normalizedActionType, string? normalizedActionValue, CancellationToken ct)
    {
        if (!string.Equals(normalizedActionType, NewsBannerManagement.ActionTypeDiscountedOffer, StringComparison.Ordinal))
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

    private async Task<BannerImageUploadSetResult> SaveBannerImagesAsync(IFormFile? imageFile, IFormFile? imageFileEn, IFormFile? imageFileRu, IFormFile? imageFileUz, bool requireDefaultImage, CancellationToken ct)
    {
        var savedPaths = new List<string>();

        var defaultResult = await SaveBannerImageSlotAsync(imageFile, ImageSlots[0], requireDefaultImage, savedPaths, ct);
        if (!defaultResult.Ok)
            return new BannerImageUploadSetResult(false, null, defaultResult.Failure);

        var englishResult = await SaveBannerImageSlotAsync(imageFileEn, ImageSlots[1], required: false, savedPaths, ct);
        if (!englishResult.Ok)
            return new BannerImageUploadSetResult(false, null, englishResult.Failure);

        var russianResult = await SaveBannerImageSlotAsync(imageFileRu, ImageSlots[2], required: false, savedPaths, ct);
        if (!russianResult.Ok)
            return new BannerImageUploadSetResult(false, null, russianResult.Failure);

        var uzbekResult = await SaveBannerImageSlotAsync(imageFileUz, ImageSlots[3], required: false, savedPaths, ct);
        if (!uzbekResult.Ok)
            return new BannerImageUploadSetResult(false, null, uzbekResult.Failure);

        return new BannerImageUploadSetResult(
            true,
            new BannerImagePaths(
                defaultResult.PublicImagePath,
                englishResult.PublicImagePath,
                russianResult.PublicImagePath,
                uzbekResult.PublicImagePath),
            null);
    }

    private async Task<BannerImageSlotUploadResult> SaveBannerImageSlotAsync(IFormFile? imageFile, BannerImageSlot slot, bool required, List<string> savedPaths, CancellationToken ct)
    {
        if (!required && (imageFile is null || imageFile.Length <= 0))
            return new BannerImageSlotUploadResult(true, null, null);

        var saveResult = await NewsBannerManagement.SaveImageAsync(imageFile, _env, ct);
        if (!saveResult.Ok)
        {
            NewsBannerManagement.DeleteImagesIfExists(_env, savedPaths);
            return new BannerImageSlotUploadResult(false, null, new BannerImageUploadFailure(slot, saveResult));
        }

        if (!string.IsNullOrWhiteSpace(saveResult.PublicImagePath))
            savedPaths.Add(saveResult.PublicImagePath);

        return new BannerImageSlotUploadResult(true, saveResult.PublicImagePath, null);
    }
}

