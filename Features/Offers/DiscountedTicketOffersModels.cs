namespace MiniApp.Features.Offers;

public sealed record DiscountedTicketOfferDto(
    long Id,
    long DrawId,
    int NumberOfDiscountedTickets,
    decimal Cost);

