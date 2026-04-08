namespace MiniApp.Features.Localization;

public sealed record LocalizationBootstrapRequest(string InitData, string? Locale);

public sealed record LocalizationBootstrapResult(bool Ok, string Locale, string Version, IReadOnlyDictionary<string, string> Strings, string? Error = null);

public sealed record LocalizationAdminItem(string Key, string EnglishValue, string RussianValue, string UzbekValue, DateTimeOffset UpdatedAtUtc);

public sealed record LocalizationAdminUpdateRequest(string Key, string EnglishValue, string RussianValue, string UzbekValue);

public sealed record LocalizationAdminUpdateResult(bool Ok, string? Error = null);

