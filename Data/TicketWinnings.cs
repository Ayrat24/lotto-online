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

        // The shared (pari-mutuel) prize is computed and locked onto the ticket when the draw
        // is executed (see DrawManagement.ExecuteDrawAsync), so read the persisted amount rather
        // than recomputing the full tier pool here.
        return decimal.Round(ticket.WinningAmount, 2, MidpointRounding.AwayFromZero);
    }

    private static HashSet<int> ParseNumberSet(string numbers)
    {
        return numbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToHashSet();
    }
}

