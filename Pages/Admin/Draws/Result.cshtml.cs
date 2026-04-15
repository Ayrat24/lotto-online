using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin.Draws;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class ResultModel : MiniApp.Pages.Admin.LocalizedAdminPageModel
{
    private sealed record TicketMatchRow(
        long UserId,
        long TelegramUserId,
        string? Number,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset LastSeenAtUtc,
        int MatchCount);

    public sealed record TierWinnersSection(
        string TierLabel,
        decimal PrizePool,
        long WinningTicketsCount,
        decimal WinningAmountPerTicket,
        IReadOnlyList<WinnerUserRow> Winners);

    public sealed record WinnerUserRow(
        long UserId,
        long TelegramUserId,
        string? Number,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset LastSeenAtUtc,
        long WinningTicketsCount,
        decimal WinningAmount);

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public ResultModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env, ILocalizationService localization)
        : base(localization)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public Draw? Draw { get; private set; }
    public IReadOnlyList<TierWinnersSection> Tiers { get; private set; } = Array.Empty<TierWinnersSection>();
    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    public async Task OnGetAsync(long id, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);

        Draw = await _db.Draws
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        if (Draw is null)
        {
            var template = await GetTextAsync("admin.draws.result.flash.notFound", "Draw #{0} was not found.", ct);
            StatusMessage = string.Format(template, id);
            StatusIsError = true;
            return;
        }

        if (Draw.State != DrawState.Finished || string.IsNullOrWhiteSpace(Draw.Numbers))
        {
            StatusMessage = await GetTextAsync("admin.draws.result.flash.finishedOnly", "Results are available only for finished draws.", ct);
            StatusIsError = true;
            return;
        }

        HashSet<int> drawNumbers;
        try
        {
            drawNumbers = ParseNumbers(Draw.Numbers);
        }
        catch (Exception)
        {
            StatusMessage = await GetTextAsync("admin.draws.result.flash.invalidNumbers", "Draw result numbers are invalid and cannot be processed.", ct);
            StatusIsError = true;
            return;
        }
        var tickets = await _db.Tickets
            .AsNoTracking()
            .Where(x => x.DrawId == id)
            .Select(x => new
            {
                x.UserId,
                x.Numbers,
                x.User.TelegramUserId,
                x.User.Number,
                x.User.CreatedAtUtc,
                x.User.LastSeenAtUtc
            })
            .ToListAsync(ct);

        var ticketsWithMatches = tickets
            .Select(x => new TicketMatchRow(
                x.UserId,
                x.TelegramUserId,
                x.Number,
                x.CreatedAtUtc,
                x.LastSeenAtUtc,
                CountMatches(x.Numbers, drawNumbers)))
            .ToArray();

        Tiers = new[]
        {
            BuildTier("3/5", Draw.PrizePoolMatch3, 3, ticketsWithMatches),
            BuildTier("4/5", Draw.PrizePoolMatch4, 4, ticketsWithMatches),
            BuildTier("5/5", Draw.PrizePoolMatch5, 5, ticketsWithMatches)
        };
    }

    private static TierWinnersSection BuildTier(string label, decimal prizePool, int matchCount, IReadOnlyList<TicketMatchRow> tickets)
    {
        var winnerTickets = tickets.Where(x => x.MatchCount == matchCount).ToArray();
        var winningTicketsCount = winnerTickets.LongLength;
        var amountPerTicket = winningTicketsCount == 0
            ? 0m
            : decimal.Round(prizePool / winningTicketsCount, 2, MidpointRounding.AwayFromZero);

        var winners = winnerTickets
            .GroupBy(x => new { x.UserId, x.TelegramUserId, x.Number, x.CreatedAtUtc, x.LastSeenAtUtc })
            .Select(g =>
            {
                var userWinningTickets = g.LongCount();
                return new WinnerUserRow(
                    g.Key.UserId,
                    g.Key.TelegramUserId,
                    g.Key.Number,
                    g.Key.CreatedAtUtc,
                    g.Key.LastSeenAtUtc,
                    userWinningTickets,
                    decimal.Round(amountPerTicket * userWinningTickets, 2, MidpointRounding.AwayFromZero));
            })
            .OrderByDescending(x => x.WinningAmount)
            .ThenByDescending(x => x.WinningTicketsCount)
            .ThenBy(x => x.UserId)
            .ToArray();

        return new TierWinnersSection(label, prizePool, winningTicketsCount, amountPerTicket, winners);
    }

    private static HashSet<int> ParseNumbers(string numbers)
    {
        var values = numbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToHashSet();

        return values;
    }

    private static int CountMatches(string ticketNumbers, HashSet<int> drawNumbers)
    {
        var ticketValues = ticketNumbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .ToHashSet();

        var matches = 0;
        foreach (var n in ticketValues)
        {
            if (drawNumbers.Contains(n))
                matches++;
        }

        return matches;
    }

    private async Task EnsureDebugSeedAsync(CancellationToken ct)
    {
        if (!LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            return;

        await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, ct);
    }
}



