using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniApp.Features.Payments;

namespace MiniApp.Data;

public sealed class WalletService : IWalletService
{
    private const decimal DefaultTopUpAmount = 10m;
    private readonly AppDbContext _db;
    private readonly IBtcPayClient _btcPay;
    private readonly PaymentsOptions _payments;

    public WalletService(AppDbContext db, IBtcPayClient btcPay, IOptions<PaymentsOptions> payments)
    {
        _db = db;
        _btcPay = btcPay;
        _payments = payments.Value;
    }

    public decimal TopUpAmount => DefaultTopUpAmount;

    public async Task<ServerWallet> EnsureServerWalletAsync(CancellationToken ct)
    {
        var wallet = await _db.ServerWallets.SingleOrDefaultAsync(x => x.Id == 1, ct);
        if (wallet is not null)
            return wallet;

        wallet = new ServerWallet
        {
            Id = 1,
            Balance = 0m,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _db.ServerWallets.Add(wallet);
        await _db.SaveChangesAsync(ct);
        return wallet;
    }

    public async Task<decimal> TopUpUserAsync(long userId, CancellationToken ct)
    {
        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new InvalidOperationException($"User {userId} was not found.");

        var serverWallet = await EnsureServerWalletAsync(ct);
        var now = DateTimeOffset.UtcNow;

        user.Balance = RoundAmount(user.Balance + TopUpAmount);
        serverWallet.Balance = RoundAmount(serverWallet.Balance + TopUpAmount);
        serverWallet.UpdatedAtUtc = now;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = user.Id,
            Type = WalletTransactionType.TopUp,
            UserDelta = TopUpAmount,
            UserBalanceAfter = user.Balance,
            ServerDelta = TopUpAmount,
            ServerBalanceAfter = serverWallet.Balance,
            Reference = "profile-topup",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        return user.Balance;
    }

    public async Task<WalletPurchaseResult> TryPurchaseTicketAsync(long userId, long drawId, string numbers, CancellationToken ct)
    {
        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == drawId, ct);
        if (draw is null)
            return new WalletPurchaseResult(false, 0m, "Draw was not found.");

        if (draw.State != DrawState.Active)
            return new WalletPurchaseResult(false, 0m, "Only the active draw accepts purchases.");

        if (draw.TicketCost <= 0)
            return new WalletPurchaseResult(false, 0m, "Ticket cost is not configured for this draw.");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return new WalletPurchaseResult(false, 0m, "User was not found.");

        var candidateSignature = BuildTicketSignature(numbers);
        var existingNumbers = await _db.Tickets
            .Where(x => x.UserId == userId && x.DrawId == drawId)
            .Select(x => x.Numbers)
            .AsNoTracking()
            .ToListAsync(ct);

        if (existingNumbers.Any(x => BuildTicketSignature(x) == candidateSignature))
            return new WalletPurchaseResult(false, user.Balance, "You already purchased this ticket for the current draw.");

        var cost = RoundAmount(draw.TicketCost);
        if (user.Balance < cost)
            return new WalletPurchaseResult(false, user.Balance, "Insufficient balance.");

        var serverWallet = await EnsureServerWalletAsync(ct);
        var now = DateTimeOffset.UtcNow;

        user.Balance = RoundAmount(user.Balance - cost);
        serverWallet.Balance = RoundAmount(serverWallet.Balance + cost);
        serverWallet.UpdatedAtUtc = now;

        var ticket = new Ticket
        {
            UserId = user.Id,
            DrawId = draw.Id,
            Numbers = numbers,
            NumbersSignature = candidateSignature,
            Status = TicketStatus.AwaitingDraw,
            PurchasedAtUtc = now
        };

        _db.Tickets.Add(ticket);
        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = user.Id,
            Type = WalletTransactionType.TicketPurchase,
            UserDelta = -cost,
            UserBalanceAfter = user.Balance,
            ServerDelta = cost,
            ServerBalanceAfter = serverWallet.Balance,
            Reference = $"draw:{draw.Id}",
            CreatedAtUtc = now
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_tickets_UserId_DrawId_NumbersSignature", StringComparison.OrdinalIgnoreCase) == true
                                        || ex.Message.Contains("IX_tickets_UserId_DrawId_NumbersSignature", StringComparison.OrdinalIgnoreCase))
        {
            return new WalletPurchaseResult(false, user.Balance, "You already purchased this ticket for the current draw.");
        }
        return new WalletPurchaseResult(true, user.Balance, null, ticket);
    }

    public async Task<WalletClaimResult> ClaimTicketWinningsAsync(long userId, long ticketId, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .Include(x => x.Draw)
            .SingleOrDefaultAsync(x => x.Id == ticketId && x.UserId == userId, ct);

        if (ticket is null)
            return new WalletClaimResult(false, 0m, 0m, "Ticket was not found.");

        if (ticket.Status != TicketStatus.WinningsAvailable)
            return new WalletClaimResult(false, 0m, 0m, "This ticket is not claimable.");

        if (ticket.Draw.State != DrawState.Finished)
            return new WalletClaimResult(false, 0m, 0m, "Draw is not finished yet.");

        var amount = TicketWinnings.GetWinningAmount(ticket, ticket.Draw);
        if (amount <= 0)
            return new WalletClaimResult(false, 0m, 0m, "Ticket has no winnings to claim.");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return new WalletClaimResult(false, 0m, 0m, "User was not found.");

        var serverWallet = await EnsureServerWalletAsync(ct);
        if (serverWallet.Balance < amount)
            return new WalletClaimResult(false, user.Balance, amount, "Server wallet does not have enough funds right now.");

        var now = DateTimeOffset.UtcNow;
        user.Balance = RoundAmount(user.Balance + amount);
        serverWallet.Balance = RoundAmount(serverWallet.Balance - amount);
        serverWallet.UpdatedAtUtc = now;
        ticket.Status = TicketStatus.WinningsClaimed;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = user.Id,
            Type = WalletTransactionType.WinningsClaimed,
            UserDelta = amount,
            UserBalanceAfter = user.Balance,
            ServerDelta = -amount,
            ServerBalanceAfter = serverWallet.Balance,
            Reference = $"ticket:{ticket.Id}",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        return new WalletClaimResult(true, user.Balance, amount, null);
    }

    public async Task<WalletWithdrawRequestResult> CreateWithdrawalRequestAsync(long userId, decimal amount, string number, CancellationToken ct)
    {
        var normalizedAmount = RoundAmount(amount);
        if (normalizedAmount <= 0)
            return new WalletWithdrawRequestResult(false, 0m, "Withdrawal amount must be greater than zero.");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return new WalletWithdrawRequestResult(false, 0m, "User was not found.");

        var normalizedNumber = NormalizePayoutNumber(number)
            ?? NormalizePayoutNumber(user.WalletAddress);
        if (normalizedNumber is null)
            return new WalletWithdrawRequestResult(false, 0m, "Please enter a valid payout address.");

        if (user.Balance < normalizedAmount)
            return new WalletWithdrawRequestResult(false, user.Balance, "Insufficient balance.");

        var serverWallet = await EnsureServerWalletAsync(ct);
        var now = DateTimeOffset.UtcNow;
        user.Balance = RoundAmount(user.Balance - normalizedAmount);
        user.WalletAddress = normalizedNumber;

        var request = new WithdrawalRequest
        {
            UserId = user.Id,
            Amount = normalizedAmount,
            Number = normalizedNumber,
            Status = WithdrawalRequestStatus.Pending,
            CreatedAtUtc = now
        };

        _db.WithdrawalRequests.Add(request);
        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = user.Id,
            Type = WalletTransactionType.WithdrawalRequested,
            UserDelta = -normalizedAmount,
            UserBalanceAfter = user.Balance,
            ServerDelta = 0m,
            ServerBalanceAfter = serverWallet.Balance,
            Reference = "withdrawal-request",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        return new WalletWithdrawRequestResult(true, user.Balance, null, request);
    }

    public async Task<WalletSaveAddressResult> SaveWalletAddressAsync(long userId, string address, CancellationToken ct)
    {
        var normalizedAddress = NormalizePayoutNumber(address);
        if (normalizedAddress is null)
            return new WalletSaveAddressResult(false, "Please enter a valid wallet address.");

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return new WalletSaveAddressResult(false, "User was not found.");

        user.WalletAddress = normalizedAddress;
        await _db.SaveChangesAsync(ct);
        return new WalletSaveAddressResult(true, null, normalizedAddress);
    }

    public async Task<string?> GetWalletAddressAsync(long userId, CancellationToken ct)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.WalletAddress)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<WalletHistoryEntry>> GetHistoryAsync(long userId, int limit, CancellationToken ct)
    {
        var take = Math.Clamp(limit, 1, 200);

        var deposits = await _db.CryptoDepositIntents
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new WalletHistoryEntry(
                "top_up",
                MapDepositHistoryStatus(x.Status),
                x.Amount,
                x.Currency,
                x.CreatedAtUtc,
                x.ProviderInvoiceId,
                x.LastProviderEventType))
            .ToListAsync(ct);

        var withdrawals = await _db.WithdrawalRequests
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new WalletHistoryEntry(
                "payout",
                MapWithdrawalHistoryStatus(x.Status, x.ExternalPayoutState),
                x.Amount,
                "USD",
                x.CreatedAtUtc,
                x.ExternalPayoutId,
                x.ReviewNote))
            .ToListAsync(ct);

        return deposits
            .Concat(withdrawals)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToArray();
    }

    public async Task<WalletReviewWithdrawalResult> ConfirmWithdrawalAsync(long withdrawalRequestId, string adminUsername, CancellationToken ct)
    {
        if (!_payments.Enabled)
            return new WalletReviewWithdrawalResult(false, "Payments are disabled. Enable BTCPay payouts before confirming withdrawals.");

        var request = await _db.WithdrawalRequests
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == withdrawalRequestId, ct);

        if (request is null)
            return new WalletReviewWithdrawalResult(false, "Withdrawal request was not found.");

        if (request.Status != WithdrawalRequestStatus.Pending)
            return new WalletReviewWithdrawalResult(false, "Only pending requests can be confirmed.");

        if (!string.IsNullOrWhiteSpace(request.ExternalPayoutId))
            return new WalletReviewWithdrawalResult(false, "This withdrawal already has an external payout id.");

        var serverWallet = await EnsureServerWalletAsync(ct);
        if (serverWallet.Balance < request.Amount)
            return new WalletReviewWithdrawalResult(false, "Server wallet does not have enough balance to confirm this withdrawal.");

        var payoutResult = await _btcPay.CreatePayoutAsync(new BtcPayCreatePayoutRequest(
            request.Amount,
            _payments.BtcPay.DefaultCurrency,
            request.Number,
            Reference: $"withdrawal:{request.Id}"), ct);
        if (!payoutResult.Success)
            return new WalletReviewWithdrawalResult(false, payoutResult.Error ?? "Failed to create BTCPay payout.");

        var now = DateTimeOffset.UtcNow;
        serverWallet.Balance = RoundAmount(serverWallet.Balance - request.Amount);
        serverWallet.UpdatedAtUtc = now;

        request.Status = WithdrawalRequestStatus.Confirmed;
        request.ReviewedByAdmin = string.IsNullOrWhiteSpace(adminUsername) ? "admin" : adminUsername.Trim();
        request.ReviewNote = null;
        request.ReviewedAtUtc = now;
        request.ExternalPayoutId = payoutResult.PayoutId;
        request.ExternalPayoutState = payoutResult.State;
        request.ExternalPayoutCreatedAtUtc = payoutResult.CreatedAtUtc ?? now;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = request.UserId,
            Type = WalletTransactionType.WithdrawalConfirmed,
            UserDelta = 0m,
            UserBalanceAfter = request.User.Balance,
            ServerDelta = -request.Amount,
            ServerBalanceAfter = serverWallet.Balance,
            Reference = $"withdrawal:{request.Id}",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        return new WalletReviewWithdrawalResult(true, null);
    }

    public async Task<WalletReviewWithdrawalResult> DenyWithdrawalAsync(long withdrawalRequestId, string adminUsername, string? note, CancellationToken ct)
    {
        var request = await _db.WithdrawalRequests
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == withdrawalRequestId, ct);

        if (request is null)
            return new WalletReviewWithdrawalResult(false, "Withdrawal request was not found.");

        if (request.Status != WithdrawalRequestStatus.Pending)
            return new WalletReviewWithdrawalResult(false, "Only pending requests can be denied.");

        var serverWallet = await EnsureServerWalletAsync(ct);
        var now = DateTimeOffset.UtcNow;
        request.User.Balance = RoundAmount(request.User.Balance + request.Amount);
        request.Status = WithdrawalRequestStatus.Denied;
        request.ReviewedByAdmin = string.IsNullOrWhiteSpace(adminUsername) ? "admin" : adminUsername.Trim();
        request.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        request.ReviewedAtUtc = now;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = request.UserId,
            Type = WalletTransactionType.WithdrawalDeniedRefund,
            UserDelta = request.Amount,
            UserBalanceAfter = request.User.Balance,
            ServerDelta = 0m,
            ServerBalanceAfter = serverWallet.Balance,
            Reference = $"withdrawal:{request.Id}",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
        return new WalletReviewWithdrawalResult(true, null);
    }

    private static string? NormalizePayoutNumber(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        if (trimmed.Length > 256)
            return null;

        return trimmed;
    }

    private static string MapWithdrawalHistoryStatus(WithdrawalRequestStatus status, string? externalPayoutState)
    {
        var payoutState = (externalPayoutState ?? string.Empty).Trim().ToLowerInvariant();
        var isPaid = payoutState is "completed" or "sent";
        var isRejected = payoutState is "failed" or "cancelled";

        return status switch
        {
            WithdrawalRequestStatus.Confirmed when isPaid => "paid",
            WithdrawalRequestStatus.Confirmed when isRejected => "rejected",
            WithdrawalRequestStatus.Pending => "waiting_for_admin_approval",
            WithdrawalRequestStatus.Denied => "rejected",
            WithdrawalRequestStatus.Confirmed => "waiting_for_admin_approval",
            _ => "waiting_for_admin_approval"
        };
    }

    private static string MapDepositHistoryStatus(CryptoDepositStatus status)
    {
        return status switch
        {
            CryptoDepositStatus.Credited => "paid",
            CryptoDepositStatus.Expired => "expired",
            CryptoDepositStatus.Invalid => "invalid",
            _ => "processing"
        };
    }

    private static string BuildTicketSignature(string numbers)
    {
        var parsed = numbers
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(int.Parse)
            .OrderBy(x => x)
            .ToArray();

        return string.Join(',', parsed);
    }

    private static decimal RoundAmount(decimal amount)
        => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}



