using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniApp.Admin;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class LocalizationModel : LocalizedAdminPageModel
{
    private readonly ILocalizationService _localization;

    public LocalizationModel(ILocalizationService localization) : base(localization)
    {
        _localization = localization;
    }

    public IReadOnlyList<LocalizationAdminItem> Items { get; private set; } = Array.Empty<LocalizationAdminItem>();
    public IReadOnlyList<string> AdminCoverageGaps { get; private set; } = Array.Empty<string>();

    [TempData]
    public string? Toast { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        Items = await _localization.GetAdminItemsAsync(ct);
        AdminCoverageGaps = await BuildAdminCoverageGapsAsync(ct);
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

    private async Task<IReadOnlyList<string>> BuildAdminCoverageGapsAsync(CancellationToken ct)
    {
        var expectedAdminKeys = LocalizationDefaults.Entries.Keys
            .Where(x => x.StartsWith("admin.", StringComparison.Ordinal))
            .OrderBy(x => x)
            .ToArray();

        var en = await _localization.GetDictionaryAsync("en", ct);
        var ru = await _localization.GetDictionaryAsync("ru", ct);
        var uz = await _localization.GetDictionaryAsync("uz", ct);

        var gaps = new List<string>();
        foreach (var keyName in expectedAdminKeys)
        {
            if (!en.TryGetValue(keyName, out var enValue) || string.IsNullOrWhiteSpace(enValue))
                gaps.Add($"{keyName}: missing/blank en value");
            if (!ru.TryGetValue(keyName, out var ruValue) || string.IsNullOrWhiteSpace(ruValue))
                gaps.Add($"{keyName}: missing/blank ru value");
            if (!uz.TryGetValue(keyName, out var uzValue) || string.IsNullOrWhiteSpace(uzValue))
                gaps.Add($"{keyName}: missing/blank uz value");
        }

        return gaps;
    }
}

