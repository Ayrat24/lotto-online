using MiniApp.Data;
using MiniApp.Features.Draws;

namespace MiniApp.Features.Offers;

public static class DiscountedTicketOfferManagement
{
    public const int MinTicketCount = 1;
    public const int MaxTicketCount = 50;

    public static DiscountedTicketOfferDto ToDto(DiscountedTicketOffer offer)
        => new(
            offer.Id,
            offer.DrawId,
            offer.NumberOfDiscountedTickets,
            RoundMoney(offer.Cost));

    public static bool IsAvailable(DiscountedTicketOffer offer, Draw draw, DateTimeOffset nowUtc)
    {
        if (!offer.IsActive)
            return false;

        if (offer.DrawId != draw.Id)
            return false;

        if (!DrawManagement.CanPurchase(draw, nowUtc))
            return false;

        if (offer.NumberOfDiscountedTickets < MinTicketCount || offer.NumberOfDiscountedTickets > MaxTicketCount)
            return false;

        if (offer.Cost <= 0)
            return false;

        return offer.Cost <= GetRegularTotal(draw.TicketCost, offer.NumberOfDiscountedTickets);
    }

    public static void ValidateForDraw(Draw draw, int numberOfDiscountedTickets, decimal cost)
    {
        if (draw.State == DrawState.Finished)
            throw new InvalidOperationException("Finished draws cannot have discounted offers.");

        if (numberOfDiscountedTickets < MinTicketCount || numberOfDiscountedTickets > MaxTicketCount)
            throw new InvalidOperationException($"Discounted offers must include between {MinTicketCount} and {MaxTicketCount} tickets.");

        var normalizedCost = RoundMoney(cost);
        if (normalizedCost <= 0)
            throw new InvalidOperationException("Discounted offer cost must be greater than zero.");

        var regularTotal = GetRegularTotal(draw.TicketCost, numberOfDiscountedTickets);
        if (normalizedCost > regularTotal)
            throw new InvalidOperationException("Discounted offer cost cannot exceed the regular total ticket price for this draw.");
    }

    public static decimal GetRegularTotal(decimal ticketCost, int ticketCount)
        => RoundMoney(RoundMoney(ticketCost) * ticketCount);

    public static decimal RoundMoney(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

