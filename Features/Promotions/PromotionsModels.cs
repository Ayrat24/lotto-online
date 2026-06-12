using MiniApp.Features.Offers;

namespace MiniApp.Features.Promotions;

public sealed record PromotionDto(
    long Id,
    string Title,
    string Subtitle,
    string ButtonText,
    string ActionType,
    string? ActionValue,
    string CardStyle,
    DiscountedTicketOfferDto? Offer = null);

public sealed record PromotionsRequest(string? InitData, string? Locale);

public sealed record PromotionsListResult(bool Ok, IReadOnlyList<PromotionDto> Promotions);
