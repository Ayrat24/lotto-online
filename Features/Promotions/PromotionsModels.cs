namespace MiniApp.Features.Promotions;

public sealed record PromotionDto(
    long Id,
    string Title,
    string Subtitle,
    string ButtonText,
    string ActionType,
    string? ActionValue,
    string BackgroundColor);

public sealed record PromotionsRequest(string? InitData, string? Locale);

public sealed record PromotionsListResult(bool Ok, IReadOnlyList<PromotionDto> Promotions);
