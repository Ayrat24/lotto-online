using MiniApp.Data;
using MiniApp.Features.Offers;

namespace MiniApp.Features.Promotions;

public static class PromotionsManagement
{
    public const string ActionTypeNone = "none";
    public const string ActionTypeAppSection = "app_section";
    public const string ActionTypeExternalUrl = "external_url";
    public const string ActionTypeDiscountedOffer = "discounted_offer";

    public const string CardStyleGold = "gold";
    public const string CardStyleDark = "dark";
    public const string CardStyleRed = "red";

    private static readonly string[] SupportedAppSections = ["home", "tickets", "winners", "profile", "deposit", "withdraw", "invite"];

    public static string NormalizeStoredActionType(string? value)
    {
        var normalized = String(value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "none" => ActionTypeNone,
            "app_section" => ActionTypeAppSection,
            "appsection" => ActionTypeAppSection,
            "external_url" => ActionTypeExternalUrl,
            "externalurl" => ActionTypeExternalUrl,
            "discounted_offer" => ActionTypeDiscountedOffer,
            "discountedoffer" => ActionTypeDiscountedOffer,
            _ => ActionTypeNone
        };
    }

    public static string? NormalizeStoredActionValue(string actionType, string? value)
    {
        var normalized = NormalizeStoredActionType(actionType);
        if (normalized == ActionTypeNone) return null;
        return value;
    }

    public static string NormalizeCardStyle(string? value)
    {
        var v = (value ?? string.Empty).Trim().ToLowerInvariant();
        return v switch
        {
            "gold" => CardStyleGold,
            "dark" => CardStyleDark,
            "red" => CardStyleRed,
            _ => CardStyleGold
        };
    }

    public static bool TryNormalizeAction(string? actionType, string? actionValue, out string normalizedType, out string? normalizedValue, out string validationError)
    {
        normalizedType = ActionTypeNone;
        normalizedValue = null;
        validationError = string.Empty;

        var typeNormalized = NormalizeStoredActionType(actionType);
        if (!IsValidActionType(typeNormalized))
        {
            validationError = "invalid_type";
            return false;
        }

        normalizedType = typeNormalized;

        if (typeNormalized == ActionTypeNone)
        {
            normalizedValue = null;
            return true;
        }

        var valueNormalized = String(actionValue ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(valueNormalized))
        {
            validationError = "missing_value";
            return false;
        }

        if (typeNormalized == ActionTypeAppSection)
        {
            if (!IsValidAppSection(valueNormalized))
            {
                validationError = "invalid_app_section";
                return false;
            }
            normalizedValue = valueNormalized.ToLowerInvariant();
            return true;
        }

        if (typeNormalized == ActionTypeExternalUrl)
        {
            if (!Uri.TryCreate(valueNormalized, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                validationError = "invalid_external_url";
                return false;
            }
            normalizedValue = uri.ToString();
            return true;
        }

        if (typeNormalized == ActionTypeDiscountedOffer)
        {
            if (!long.TryParse(valueNormalized, out var offerId) || offerId <= 0)
            {
                validationError = "invalid_discounted_offer";
                return false;
            }
            normalizedValue = offerId.ToString();
            return true;
        }

        validationError = "invalid_type";
        return false;
    }

    public static bool IsValidActionType(string actionType)
    {
        var normalized = NormalizeStoredActionType(actionType);
        return normalized == ActionTypeNone
            || normalized == ActionTypeAppSection
            || normalized == ActionTypeExternalUrl
            || normalized == ActionTypeDiscountedOffer;
    }

    public static bool IsValidAppSection(string appSection)
    {
        var normalized = String(appSection ?? string.Empty).Trim().ToLowerInvariant();
        return SupportedAppSections.Contains(normalized);
    }

    public static IReadOnlyList<string> GetSupportedAppSections()
    {
        return SupportedAppSections;
    }

    public static PromotionDto ToDto(Promotion promotion, string? locale = null, DiscountedTicketOfferDto? offer = null)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var normalizedType = NormalizeStoredActionType(promotion.ActionType);
        var resolvedOffer = string.Equals(normalizedType, ActionTypeDiscountedOffer, StringComparison.Ordinal) ? offer : null;
        return new PromotionDto(
            promotion.Id,
            ResolveText(promotion.Title, promotion.TitleRu, promotion.TitleUz, normalizedLocale),
            ResolveText(promotion.Subtitle, promotion.SubtitleRu, promotion.SubtitleUz, normalizedLocale),
            ResolveText(promotion.ButtonText, promotion.ButtonTextRu, promotion.ButtonTextUz, normalizedLocale),
            normalizedType,
            resolvedOffer is not null ? resolvedOffer.Id.ToString() : NormalizeStoredActionValue(promotion.ActionType, promotion.ActionValue),
            NormalizeCardStyle(promotion.CardStyle),
            resolvedOffer);
    }

    private static string NormalizeLocale(string? locale)
    {
        var v = (locale ?? string.Empty).Trim().ToLowerInvariant();
        if (v.StartsWith("ru", StringComparison.Ordinal)) return "ru";
        if (v.StartsWith("uz", StringComparison.Ordinal)) return "uz";
        return "en";
    }

    private static string ResolveText(string en, string ru, string uz, string normalizedLocale)
    {
        var localized = normalizedLocale switch
        {
            "ru" => ru,
            "uz" => uz,
            _ => en
        };
        return string.IsNullOrWhiteSpace(localized) ? en : localized;
    }

    private static string String(string value) => value ?? string.Empty;
}
