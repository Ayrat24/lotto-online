using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniApp.Data;

namespace MiniApp.Features.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public LocalizationService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public IReadOnlyList<string> SupportedLocales { get; } = new[] { "en", "ru", "uz" };

    public string NormalizeLocale(string? locale)
    {
        var value = (locale ?? string.Empty).Trim().ToLowerInvariant();
        if (value.StartsWith("ru", StringComparison.Ordinal)) return "ru";
        if (value.StartsWith("uz", StringComparison.Ordinal)) return "uz";
        return "en";
    }

    public async Task EnsureDefaultsAsync(CancellationToken ct)
    {
        var hasAny = await _db.LocalizationTexts.AsNoTracking().AnyAsync(ct);
        if (hasAny)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var pair in LocalizationDefaults.Entries)
        {
            _db.LocalizationTexts.Add(new LocalizationText
            {
                Key = pair.Key,
                EnglishValue = pair.Value.En,
                RussianValue = pair.Value.Ru,
                UzbekValue = pair.Value.Uz,
                UpdatedAtUtc = now
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetDictionaryAsync(string locale, CancellationToken ct)
    {
        await EnsureDefaultsAsync(ct);

        var normalizedLocale = NormalizeLocale(locale);
        var cacheKey = $"loc:dict:{normalizedLocale}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, string>? cached) && cached is not null)
            return cached;

        var rows = await _db.LocalizationTexts
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .ToListAsync(ct);

        var dict = rows.ToDictionary(
            x => x.Key,
            x => normalizedLocale switch
            {
                "ru" => x.RussianValue,
                "uz" => x.UzbekValue,
                _ => x.EnglishValue
            },
            StringComparer.Ordinal);

        _cache.Set(cacheKey, dict, TimeSpan.FromMinutes(1));
        return dict;
    }

    public async Task<string> GetVersionAsync(CancellationToken ct)
    {
        await EnsureDefaultsAsync(ct);

        var ticks = await _db.LocalizationTexts
            .AsNoTracking()
            .Select(x => (long?)x.UpdatedAtUtc.UtcTicks)
            .MaxAsync(ct) ?? 0L;

        return ticks.ToString();
    }

    public async Task<string> GetTextAsync(string locale, string key, string fallback, CancellationToken ct)
    {
        var dict = await GetDictionaryAsync(locale, ct);
        return dict.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    public async Task<IReadOnlyList<LocalizationAdminItem>> GetAdminItemsAsync(CancellationToken ct)
    {
        await EnsureDefaultsAsync(ct);

        var items = await _db.LocalizationTexts
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new LocalizationAdminItem(
                x.Key,
                x.EnglishValue,
                x.RussianValue,
                x.UzbekValue,
                x.UpdatedAtUtc))
            .ToListAsync(ct);

        return items;
    }

    public async Task<LocalizationAdminUpdateResult> UpsertAsync(LocalizationAdminUpdateRequest request, CancellationToken ct)
    {
        var key = request.Key.Trim();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
            return new LocalizationAdminUpdateResult(false, "Localization key is invalid.");

        var english = request.EnglishValue.Trim();
        var russian = request.RussianValue.Trim();
        var uzbek = request.UzbekValue.Trim();

        if (string.IsNullOrWhiteSpace(english) || string.IsNullOrWhiteSpace(russian) || string.IsNullOrWhiteSpace(uzbek))
            return new LocalizationAdminUpdateResult(false, "All locale values are required.");

        if (english.Length > 2048 || russian.Length > 2048 || uzbek.Length > 2048)
            return new LocalizationAdminUpdateResult(false, "One of values is too long.");

        var now = DateTimeOffset.UtcNow;
        var row = await _db.LocalizationTexts.SingleOrDefaultAsync(x => x.Key == key, ct);
        if (row is null)
        {
            row = new LocalizationText
            {
                Key = key,
                EnglishValue = english,
                RussianValue = russian,
                UzbekValue = uzbek,
                UpdatedAtUtc = now
            };
            _db.LocalizationTexts.Add(row);
        }
        else
        {
            row.EnglishValue = english;
            row.RussianValue = russian;
            row.UzbekValue = uzbek;
            row.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(ct);

        _cache.Remove("loc:dict:en");
        _cache.Remove("loc:dict:ru");
        _cache.Remove("loc:dict:uz");

        return new LocalizationAdminUpdateResult(true);
    }
}


