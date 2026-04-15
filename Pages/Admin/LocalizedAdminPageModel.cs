using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

public abstract class LocalizedAdminPageModel : PageModel
{
    private readonly ILocalizationService _localization;

    protected LocalizedAdminPageModel(ILocalizationService localization)
    {
        _localization = localization;
    }

    public IReadOnlyDictionary<string, string> UiText { get; private set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    protected async Task LoadUiTextAsync(CancellationToken ct)
    {
        UiText = await _localization.GetDictionaryAsync(GetAdminLocale(), ct);
    }

    protected string GetAdminLocale()
        => _localization.NormalizeLocale(Request.Cookies["AdminUiLanguage"]);

    protected Task<string> GetTextAsync(string key, string fallback, CancellationToken ct)
        => _localization.GetTextAsync(GetAdminLocale(), key, fallback, ct);

    public string T(string key, string fallback)
        => UiText.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
}

