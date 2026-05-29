using MiniApp.Data;

namespace MiniApp.Features.Promotions;

public static class PromotionsManagement
{
    public const string ActionTypeNone = "none";
    public const string ActionTypeAppSection = "app_section";
    public const string ActionTypeExternalUrl = "external_url";
    public const string ActionTypeDiscountedOffer = "discounted_offer";

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

    public static PromotionDto ToDto(Promotion promotion)
    {
        return new PromotionDto(
            promotion.Id,
            promotion.Title,
            promotion.Subtitle,
            promotion.ButtonText,
            NormalizeStoredActionType(promotion.ActionType),
            NormalizeStoredActionValue(promotion.ActionType, promotion.ActionValue),
            promotion.BackgroundColor);
    }

    private static string String(string value) => value ?? string.Empty;
}
