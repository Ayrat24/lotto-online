using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Localization;
using MiniApp.Features.Winners;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class WinnersModel : LocalizedAdminPageModel
{
    public sealed record AdminWinnerRow(
        long Id,
        string Name,
        string WinningAmount,
        string? Quote,
        string PhotoPath,
        int DisplayOrder,
        bool IsPublished,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public WinnersModel(AppDbContext db, IWebHostEnvironment env, ILocalizationService localization)
        : base(localization)
    {
        _db = db;
        _env = env;
    }

    public IReadOnlyList<AdminWinnerRow> Items { get; private set; } = Array.Empty<AdminWinnerRow>();
    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }
    public int MaxUploadMegabytes => (int)Math.Ceiling(WinnerManagement.MaxUploadBytes / 1024d / 1024d);

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

    public async Task<IActionResult> OnPostCreateAsync(IFormFile? photoFile, string? name, string? winningAmount, string? quote, int displayOrder, bool isPublished, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var normalizedName = name?.Trim() ?? string.Empty;
        if (normalizedName.Length == 0)
        {
            await SetFlashAsync(await GetTextAsync("admin.winners.flash.nameRequired", "Enter a winner name.", ct), isError: true);
            return RedirectToPage();
        }

        var normalizedWinningAmount = winningAmount?.Trim() ?? string.Empty;
        if (normalizedWinningAmount.Length == 0)
        {
            await SetFlashAsync(await GetTextAsync("admin.winners.flash.amountRequired", "Enter a win amount.", ct), isError: true);
            return RedirectToPage();
        }

        var saveResult = await WinnerManagement.SaveImageAsync(photoFile, _env, ct);
        if (!saveResult.Ok)
        {
            await SetFlashAsync(await GetUploadErrorMessageAsync(saveResult, ct), isError: true);
            return RedirectToPage();
        }

        var now = DateTimeOffset.UtcNow;
        var entry = new WinnerEntry
        {
            Name = normalizedName,
            WinningAmountText = normalizedWinningAmount,
            QuoteText = string.IsNullOrWhiteSpace(quote) ? null : quote.Trim(),
            PhotoPath = saveResult.PublicImagePath!,
            DisplayOrder = Math.Max(0, displayOrder),
            IsPublished = isPublished,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.WinnerEntries.Add(entry);

        try
        {
            await _db.SaveChangesAsync(ct);
            var template = await GetTextAsync("admin.winners.flash.created", "Winner #{0} created.", ct);
            await SetFlashAsync(string.Format(template, entry.Id), isError: false);
        }
        catch
        {
            WinnerManagement.DeleteImageIfExists(_env, saveResult.PublicImagePath);
            await SetFlashAsync(await GetTextAsync("admin.winners.flash.uploadFailed", "Winner photo upload failed.", ct), isError: true);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(long id, string? name, string? winningAmount, string? quote, int displayOrder, bool isPublished, IFormFile? photoFile, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var normalizedName = name?.Trim() ?? string.Empty;
        if (normalizedName.Length == 0)
        {
            await SetFlashAsync(await GetTextAsync("admin.winners.flash.nameRequired", "Enter a winner name.", ct), isError: true);
            return RedirectToPage();
        }

        var normalizedWinningAmount = winningAmount?.Trim() ?? string.Empty;
        if (normalizedWinningAmount.Length == 0)
        {
            await SetFlashAsync(await GetTextAsync("admin.winners.flash.amountRequired", "Enter a win amount.", ct), isError: true);
            return RedirectToPage();
        }

        var entry = await _db.WinnerEntries.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null)
        {
            var notFound = await GetTextAsync("admin.winners.flash.notFound", "Winner entry was not found.", ct);
            await SetFlashAsync(notFound, isError: true);
            return RedirectToPage();
        }

        string? previousPhotoPath = null;
        string? replacementPhotoPath = null;
        if (photoFile is not null && photoFile.Length > 0)
        {
            var saveResult = await WinnerManagement.SaveImageAsync(photoFile, _env, ct);
            if (!saveResult.Ok)
            {
                await SetFlashAsync(await GetUploadErrorMessageAsync(saveResult, ct), isError: true);
                return RedirectToPage();
            }

            previousPhotoPath = entry.PhotoPath;
            replacementPhotoPath = saveResult.PublicImagePath;
            entry.PhotoPath = saveResult.PublicImagePath!;
        }

        entry.Name = normalizedName;
        entry.WinningAmountText = normalizedWinningAmount;
        entry.QuoteText = string.IsNullOrWhiteSpace(quote) ? null : quote.Trim();
        entry.DisplayOrder = Math.Max(0, displayOrder);
        entry.IsPublished = isPublished;
        entry.UpdatedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(replacementPhotoPath))
                WinnerManagement.DeleteImageIfExists(_env, replacementPhotoPath);

            await SetFlashAsync(await GetTextAsync("admin.winners.flash.uploadFailed", "Winner photo upload failed.", ct), isError: true);
            return RedirectToPage();
        }

        if (!string.IsNullOrWhiteSpace(previousPhotoPath) && !string.Equals(previousPhotoPath, entry.PhotoPath, StringComparison.Ordinal))
            WinnerManagement.DeleteImageIfExists(_env, previousPhotoPath);

        var template = await GetTextAsync("admin.winners.flash.updated", "Winner #{0} updated.", ct);
        await SetFlashAsync(string.Format(template, entry.Id), isError: false);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);

        var entry = await _db.WinnerEntries.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null)
        {
            var notFound = await GetTextAsync("admin.winners.flash.notFound", "Winner entry was not found.", ct);
            await SetFlashAsync(notFound, isError: true);
            return RedirectToPage();
        }

        var photoPath = entry.PhotoPath;
        _db.WinnerEntries.Remove(entry);
        await _db.SaveChangesAsync(ct);
        WinnerManagement.DeleteImageIfExists(_env, photoPath);

        var template = await GetTextAsync("admin.winners.flash.deleted", "Winner #{0} deleted.", ct);
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
        Items = await _db.WinnerEntries
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new AdminWinnerRow(
                x.Id,
                x.Name,
                x.WinningAmountText,
                x.QuoteText,
                x.PhotoPath,
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

    private async Task<string> GetUploadErrorMessageAsync(WinnerImageSaveResult result, CancellationToken ct)
    {
        return result.Error switch
        {
            WinnerImageError.MissingFile => await GetTextAsync("admin.winners.flash.uploadMissing", "Select an image to upload.", ct),
            WinnerImageError.InvalidType => await GetTextAsync("admin.winners.flash.uploadInvalidType", "Only .jpg, .jpeg, .png, or .webp images are allowed.", ct),
            WinnerImageError.FileTooLarge => string.Format(
                await GetTextAsync("admin.winners.flash.uploadFileTooLarge", "Winner photo is too large. Keep it under {0} MB.", ct),
                MaxUploadMegabytes),
            _ => await GetTextAsync("admin.winners.flash.uploadFailed", "Winner photo upload failed.", ct)
        };
    }
}


