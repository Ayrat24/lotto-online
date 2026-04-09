using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniApp.Data;

namespace MiniApp.Features.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly IReadOnlyDictionary<string, string> LegacyRussianValues = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["client.tab.home"] = "Glavnaya",
        ["client.tab.tickets"] = "Moi bilety",
        ["client.tab.profile"] = "Profil",
        ["client.balance.label"] = "Balans",
        ["client.button.purchase"] = "Kupit bilet",
        ["client.button.deposit"] = "Popolnit crypto",
        ["client.button.withdraw"] = "Vyvesti",
        ["client.button.saveWallet"] = "Sohranit adres",
        ["client.status.loadingHistory"] = "Zagruzka istorii tranzaktsiy...",
        ["client.status.noActiveDraw"] = "Seychas net aktivnogo tirazha.",
        ["client.status.ticketPurchased"] = "Bilet kuplen.",
        ["client.status.purchaseFailed"] = "Pokupka ne udalas.",
        ["client.status.authenticationFailed"] = "Oshibka avtorizatsii.",
        ["client.picker.preparing"] = "Podgotovka chisel...",
        ["client.picker.chooseUnique"] = "Vyberite 5 unikalnyh chisel.",
        ["client.picker.submitting"] = "Otpravka bileta...",
        ["client.picker.confirm"] = "Podtverdit chisla",
        ["admin.nav.localization"] = "Lokalizatsiya",
        ["admin.localization.title"] = "Lokalizatsiya",
        ["admin.localization.subtitle"] = "Obnovite stroki en / ru / uz.",
        ["bot.welcomeBack"] = "S vozvrashcheniem!",
        ["bot.openMiniApp"] = "Otkryt Mini App",
        ["bot.changeLanguage"] = "Smenit yazyk",
        ["bot.tapOpenMiniApp"] = "Nazhmi knopku nizhe chtoby otkryt Mini App.",
        ["bot.askLanguage"] = "Pozhaluysta vyberite yazyk:",
        ["bot.askContact"] = "Pozhaluysta otpravte kontakt chtoby prodolzhit.",
        ["bot.shareContact"] = "Podelitsya kontaktom",
        ["bot.savedNumber"] = "Spasibo! Sohraneno.",
        ["bot.invalidNumber"] = "Ne udalos raspoznat nomer. Nazhmite 'Podelitsya kontaktom' ili otpravte nomer tekstom.",
        ["bot.languageUpdated"] = "Yazyk obnovlen.",
        ["bot.languageBeforePhone"] = "Snachala vyberite yazyk.",
        ["bot.askPhonePlaceholder"] = "Nazhmi knopku chtoby podelitsya nomerom"
    };

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
        var now = DateTimeOffset.UtcNow;
        var existing = await _db.LocalizationTexts.ToDictionaryAsync(x => x.Key, StringComparer.Ordinal, ct);
        var changed = false;

        foreach (var pair in LocalizationDefaults.Entries)
        {
            if (!existing.TryGetValue(pair.Key, out var row))
            {
                _db.LocalizationTexts.Add(new LocalizationText
                {
                    Key = pair.Key,
                    EnglishValue = pair.Value.En,
                    RussianValue = pair.Value.Ru,
                    UzbekValue = pair.Value.Uz,
                    UpdatedAtUtc = now
                });
                changed = true;
                continue;
            }

            var updated = false;
            if (string.IsNullOrWhiteSpace(row.EnglishValue))
            {
                row.EnglishValue = pair.Value.En;
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(row.RussianValue))
            {
                row.RussianValue = pair.Value.Ru;
                updated = true;
            }
            else if (string.Equals(row.RussianValue, row.EnglishValue, StringComparison.Ordinal)
                     && string.Equals(row.EnglishValue, pair.Value.En, StringComparison.Ordinal)
                     && !string.Equals(pair.Value.Ru, pair.Value.En, StringComparison.Ordinal))
            {
                // Repair legacy rows where RU was seeded from EN by mistake.
                row.RussianValue = pair.Value.Ru;
                updated = true;
            }
            else if (LegacyRussianValues.TryGetValue(pair.Key, out var legacyRussian)
                     && string.Equals(row.RussianValue, legacyRussian, StringComparison.Ordinal))
            {
                row.RussianValue = pair.Value.Ru;
                updated = true;
            }

            if (string.IsNullOrWhiteSpace(row.UzbekValue))
            {
                row.UzbekValue = pair.Value.Uz;
                updated = true;
            }
            else if (string.Equals(row.UzbekValue, row.EnglishValue, StringComparison.Ordinal)
                     && string.Equals(row.EnglishValue, pair.Value.En, StringComparison.Ordinal)
                     && !string.Equals(pair.Value.Uz, pair.Value.En, StringComparison.Ordinal))
            {
                // Repair legacy rows where UZ was seeded from EN by mistake.
                row.UzbekValue = pair.Value.Uz;
                updated = true;
            }

            if (updated)
            {
                row.UpdatedAtUtc = now;
                changed = true;
            }
        }

        if (changed)
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
            .Where(x => !x.Key.StartsWith("admin."))
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

        if (key.StartsWith("admin.", StringComparison.OrdinalIgnoreCase))
            return new LocalizationAdminUpdateResult(false, "Admin UI localization keys are read-only.");

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


