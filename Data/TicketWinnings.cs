namespace MiniApp.Data;

public static class TicketWinnings
{
    public static int GetMatchCount(string ticketNumbers, string drawNumbers)
    {
        var drawSet = ParseNumberSet(drawNumbers);
        var ticketSet = ParseNumberSet(ticketNumbers);

        var matches = 0;
        foreach (var n in ticketSet)
        {
            if (drawSet.Contains(n))
                matches++;
        }

        return matches;
    }

    public static decimal GetWinningAmount(Ticket ticket, Draw draw)
    {
        if (ticket.Status != TicketStatus.WinningsAvailable && ticket.Status != TicketStatus.WinningsClaimed)
            return 0m;

        if (draw.State != DrawState.Finished || string.IsNullOrWhiteSpace(draw.Numbers))
            return 0m;

        var matchCount = GetMatchCount(ticket.Numbers, draw.Numbers);

        var amount = matchCount switch
        {
            3 => draw.PrizePoolMatch3,
            4 => draw.PrizePoolMatch4,
            5 => draw.PrizePoolMatch5,
            _ => 0m
        };

        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private static HashSet<int> ParseNumberSet(string numbers)
    {
        return numbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToHashSet();
    }
}

