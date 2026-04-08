namespace MiniApp.Features.Localization;

public interface ILocalizationService
{
    IReadOnlyList<string> SupportedLocales { get; }

    string NormalizeLocale(string? locale);

    Task EnsureDefaultsAsync(CancellationToken ct);

    Task<IReadOnlyDictionary<string, string>> GetDictionaryAsync(string locale, CancellationToken ct);

    Task<string> GetVersionAsync(CancellationToken ct);

    Task<string> GetTextAsync(string locale, string key, string fallback, CancellationToken ct);

    Task<IReadOnlyList<LocalizationAdminItem>> GetAdminItemsAsync(CancellationToken ct);

    Task<LocalizationAdminUpdateResult> UpsertAsync(LocalizationAdminUpdateRequest request, CancellationToken ct);
}

