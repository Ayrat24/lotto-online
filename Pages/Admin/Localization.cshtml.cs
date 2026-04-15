using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniApp.Admin;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class LocalizationModel : PageModel
{
    private readonly ILocalizationService _localization;

    public LocalizationModel(ILocalizationService localization)
    {
        _localization = localization;
    }

    public IReadOnlyList<LocalizationAdminItem> Items { get; private set; } = Array.Empty<LocalizationAdminItem>();
    public string TitleText { get; private set; } = "Localization";
    public string SubtitleText { get; private set; } = "Update en / ru / uz UI strings.";
    public string KeyColumnText { get; private set; } = "Key";
    public string EnglishColumnText { get; private set; } = "English";
    public string RussianColumnText { get; private set; } = "Russian";
    public string UzbekColumnText { get; private set; } = "Uzbek";
    public string SaveActionText { get; private set; } = "Save";

    [TempData]
    public string? Toast { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        Items = await _localization.GetAdminItemsAsync(ct);
    }

    private async Task LoadUiTextAsync(CancellationToken ct)
    {
        var locale = _localization.NormalizeLocale(Request.Cookies["AdminUiLanguage"]);
        TitleText = await _localization.GetTextAsync(locale, "admin.localization.title", TitleText, ct);
        SubtitleText = await _localization.GetTextAsync(locale, "admin.localization.subtitle", SubtitleText, ct);
        KeyColumnText = await _localization.GetTextAsync(locale, "admin.localization.column.key", KeyColumnText, ct);
        EnglishColumnText = await _localization.GetTextAsync(locale, "admin.localization.column.english", EnglishColumnText, ct);
        RussianColumnText = await _localization.GetTextAsync(locale, "admin.localization.column.russian", RussianColumnText, ct);
        UzbekColumnText = await _localization.GetTextAsync(locale, "admin.localization.column.uzbek", UzbekColumnText, ct);
        SaveActionText = await _localization.GetTextAsync(locale, "admin.localization.action.save", SaveActionText, ct);
    }

    public async Task<IActionResult> OnPostSaveAsync(string key, string englishValue, string russianValue, string uzbekValue, CancellationToken ct)
    {
        var result = await _localization.UpsertAsync(new LocalizationAdminUpdateRequest(
            key,
            englishValue,
            russianValue,
            uzbekValue), ct);

        Toast = result.Ok ? $"Saved '{key}'." : result.Error;
        return RedirectToPage();
    }
}

