using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Localization;
using MiniApp.Features.NewsBanners;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class NewsBannersModel : LocalizedAdminPageModel
{
    public sealed record AdminNewsBannerRow(
        long Id,
        string ImagePath,
        string ActionType,
        string? ActionValue,
        int DisplayOrder,
        bool IsPublished,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    public sealed record ActionTypeOption(string Value, string LabelKey, string FallbackLabel);

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
        new(NewsBannerManagement.ActionTypeExternalUrl, "admin.newsBanners.actionType.externalUrl", "Open external link")
    ];

    public IReadOnlyList<string> AppSectionOptions { get; } = NewsBannerManagement.GetSupportedAppSections();
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

    public async Task<IActionResult> OnPostCreateAsync(IFormFile? imageFile, int displayOrder, bool isPublished, string actionType, string? actionValue, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        if (!NewsBannerManagement.TryNormalizeAction(actionType, actionValue, out var normalizedActionType, out var normalizedActionValue, out var actionValidationError))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync(actionValidationError, ct), isError: true);
            return RedirectToPage();
        }

        var saveResult = await NewsBannerManagement.SaveImageAsync(imageFile, _env, ct);
        if (!saveResult.Ok)
        {
            await SetFlashAsync(await GetUploadErrorMessageAsync(saveResult, ct), isError: true);
            return RedirectToPage();
        }

        var now = DateTimeOffset.UtcNow;
        var banner = new NewsBanner
        {
            ImagePath = saveResult.PublicImagePath!,
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
            NewsBannerManagement.DeleteImageIfExists(_env, saveResult.PublicImagePath);
            await SetFlashAsync(await GetTextAsync("admin.newsBanners.flash.uploadFailed", "Banner upload failed.", ct), isError: true);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(long id, int displayOrder, bool isPublished, string actionType, string? actionValue, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        if (!NewsBannerManagement.TryNormalizeAction(actionType, actionValue, out var normalizedActionType, out var normalizedActionValue, out var actionValidationError))
        {
            await SetFlashAsync(await GetActionValidationMessageAsync(actionValidationError, ct), isError: true);
            return RedirectToPage();
        }

        var banner = await _db.NewsBanners.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (banner is null)
        {
            var notFound = await GetTextAsync("admin.newsBanners.flash.notFound", "Banner was not found.", ct);
            await SetFlashAsync(notFound, isError: true);
            return RedirectToPage();
        }

        banner.DisplayOrder = Math.Max(0, displayOrder);
        banner.IsPublished = isPublished;
        banner.ActionType = normalizedActionType;
        banner.ActionValue = normalizedActionValue;
        banner.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

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

        var imagePath = banner.ImagePath;
        _db.NewsBanners.Remove(banner);
        await _db.SaveChangesAsync(ct);
        NewsBannerManagement.DeleteImageIfExists(_env, imagePath);

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
        Items = await _db.NewsBanners
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminNewsBannerRow(
                x.Id,
                x.ImagePath,
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

    private async Task<string> GetUploadErrorMessageAsync(NewsBannerImageSaveResult result, CancellationToken ct)
    {
        return result.Error switch
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
    }

    private async Task<string> GetActionValidationMessageAsync(string validationErrorCode, CancellationToken ct)
    {
        return validationErrorCode switch
        {
            "invalid_type" => await GetTextAsync("admin.newsBanners.flash.actionTypeInvalid", "Select a valid banner action.", ct),
            "missing_value" => await GetTextAsync("admin.newsBanners.flash.actionValueRequired", "Enter the action target for this banner.", ct),
            "invalid_app_section" => await GetTextAsync("admin.newsBanners.flash.actionAppSectionInvalid", "Choose a valid app section target.", ct),
            "invalid_external_url" => await GetTextAsync("admin.newsBanners.flash.actionUrlInvalid", "Enter a valid absolute HTTPS URL.", ct),
            _ => await GetTextAsync("admin.newsBanners.flash.actionTypeInvalid", "Select a valid banner action.", ct)
        };
    }
}

