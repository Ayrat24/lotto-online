using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniApp.Features.Offers;
using MiniApp.Features.Payments;
using Npgsql;
using TonSdk.Core;

namespace MiniApp.Data;

public sealed class WalletService : IWalletService
{
    private const string TonWithdrawalSupportMigrationName = "20260430053832_AddTonWithdrawalSupport";
    private readonly AppDbContext _db;
    private readonly IBtcPayClient _btcPay;
    private readonly ITelegramTonRateService _telegramTonRate;
    private readonly PaymentsOptions _payments;

    public WalletService(AppDbContext db, IBtcPayClient btcPay, ITelegramTonRateService telegramTonRate, IOptions<PaymentsOptions> payments)
    {
        _db = db;
        _btcPay = btcPay;
        _telegramTonRate = telegramTonRate;
        _payments = payments.Value;
    }

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

    public async Task<WalletBatchPurchaseResult> TryPurchaseTicketsAsync(long userId, long drawId, IReadOnlyList<string> numbersByTicket, long? offerId, CancellationToken ct)
    {
        if (numbersByTicket.Count == 0)
            return new WalletBatchPurchaseResult(false, 0m, 0m, "Select at least one ticket first.");

        var draw = await _db.Draws.SingleOrDefaultAsync(x => x.Id == drawId, ct);
        if (draw is null)
            return new WalletBatchPurchaseResult(false, 0m, 0m, "Draw was not found.");

        if (draw.State != DrawState.Active)
            return new WalletBatchPurchaseResult(false, 0m, 0m, "Only active draws accept purchases.");

        if (draw.TicketCost <= 0)
            return new WalletBatchPurchaseResult(false, 0m, 0m, "Ticket cost is not configured for this draw.");

        DiscountedTicketOffer? offer = null;
        if (offerId.HasValue)
        {
            offer = await _db.DiscountedTicketOffers
                .Include(x => x.Draw)
                .SingleOrDefaultAsync(x => x.Id == offerId.Value, ct);

            if (offer is null)
                return new WalletBatchPurchaseResult(false, 0m, 0m, "Discounted offer was not found.");

            if (offer.DrawId != draw.Id)
                return new WalletBatchPurchaseResult(false, 0m, 0m, "Discounted offer does not belong to the selected draw.");

            if (!DiscountedTicketOfferManagement.IsAvailable(offer, draw, DateTimeOffset.UtcNow))
                return new WalletBatchPurchaseResult(false, 0m, 0m, "Discounted offer is not available right now.");

            if (numbersByTicket.Count != offer.NumberOfDiscountedTickets)
            {
                return new WalletBatchPurchaseResult(
                    false,
                    0m,
                    DiscountedTicketOfferManagement.RoundMoney(offer.Cost),
                    $"Discounted offer requires exactly {offer.NumberOfDiscountedTickets} tickets.");
            }
        }

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return new WalletBatchPurchaseResult(false, 0m, 0m, "User was not found.");

        var requestedNumbers = numbersByTicket
            .Select(x => string.Join(',', x
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)))
            .ToArray();
        var requestedSignatures = requestedNumbers
            .Select(BuildTicketSignature)
            .ToArray();
        var uniqueRequestedSignatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var signature in requestedSignatures)
        {
            if (!uniqueRequestedSignatures.Add(signature))
                return new WalletBatchPurchaseResult(false, user.Balance, 0m, "Completed tickets must use different number combinations.");
        }

        var existingNumbers = await _db.Tickets
            .Where(x => x.UserId == userId && x.DrawId == drawId)
            .Select(x => x.Numbers)
            .AsNoTracking()
            .ToListAsync(ct);

        var existingSignatures = existingNumbers
            .Select(BuildTicketSignature)
            .ToHashSet(StringComparer.Ordinal);

        if (requestedSignatures.Any(existingSignatures.Contains))
            return new WalletBatchPurchaseResult(false, user.Balance, 0m, "One of the selected tickets was already purchased for the selected draw.");

        var costPerTicket = RoundAmount(draw.TicketCost);
        var totalCost = offer is null
            ? RoundAmount(costPerTicket * requestedNumbers.Length)
            : DiscountedTicketOfferManagement.RoundMoney(offer.Cost);
        if (user.Balance < totalCost)
            return new WalletBatchPurchaseResult(false, user.Balance, totalCost, "Insufficient balance.");

        var serverWallet = await EnsureServerWalletAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var balanceBeforePurchase = user.Balance;
        var serverBalanceBeforePurchase = serverWallet.Balance;

        var createdTickets = new List<Ticket>(requestedNumbers.Length);
        var userBalance = user.Balance;
        var serverBalance = serverWallet.Balance;

        foreach (var (numbers, signature) in requestedNumbers.Zip(requestedSignatures))
        {
            var ticket = new Ticket
            {
                UserId = user.Id,
                DrawId = draw.Id,
                Numbers = numbers,
                NumbersSignature = signature,
                Status = TicketStatus.AwaitingDraw,
                PurchasedAtUtc = now
            };

            createdTickets.Add(ticket);
            _db.Tickets.Add(ticket);
        }

        userBalance = RoundAmount(userBalance - totalCost);
        serverBalance = RoundAmount(serverBalance + totalCost);

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = user.Id,
            Type = WalletTransactionType.TicketPurchase,
            UserDelta = -totalCost,
            UserBalanceAfter = userBalance,
            ServerDelta = totalCost,
            ServerBalanceAfter = serverBalance,
            Reference = offer is null
                ? $"draw:{draw.Id}"
                : $"draw:{draw.Id}:offer:{offer.Id}",
            CreatedAtUtc = now
        });

        user.Balance = userBalance;
        serverWallet.Balance = serverBalance;
        serverWallet.UpdatedAtUtc = now;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_tickets_UserId_DrawId_NumbersSignature", StringComparison.OrdinalIgnoreCase) == true
                                        || ex.Message.Contains("IX_tickets_UserId_DrawId_NumbersSignature", StringComparison.OrdinalIgnoreCase))
        {
            user.Balance = balanceBeforePurchase;
            serverWallet.Balance = serverBalanceBeforePurchase;
            return new WalletBatchPurchaseResult(false, balanceBeforePurchase, totalCost, "One of the selected tickets was already purchased for the selected draw.");
        }
        return new WalletBatchPurchaseResult(true, user.Balance, totalCost, null, createdTickets);
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

    public async Task<WalletWithdrawRequestResult> CreateWithdrawalRequestAsync(long userId, decimal amount, string assetCode, string? address, bool saveAddress, CancellationToken ct)
    {
        await EnsureTonWithdrawalSchemaAsync(ct);

        var normalizedAmount = RoundAmount(amount);
        if (normalizedAmount <= 0)
            return new WalletWithdrawRequestResult(false, 0m, "Withdrawal amount must be greater than zero.");

        var normalizedAssetCode = WithdrawalAssetCodes.Normalize(assetCode, defaultToBitcoin: false);
        if (normalizedAssetCode is null)
            return new WalletWithdrawRequestResult(false, 0m, "Unsupported withdrawal asset.");

        if (normalizedAssetCode == WithdrawalAssetCodes.Ton
            && (!_payments.Enabled || !_payments.TelegramTon.Enabled || !_payments.TelegramTon.ServerWithdrawalsEnabled))
        {
            return new WalletWithdrawRequestResult(false, 0m, "TON withdrawals are unavailable right now.");
        }

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return new WalletWithdrawRequestResult(false, 0m, "User was not found.");

        string? normalizedNumber;
        if (string.IsNullOrWhiteSpace(address))
        {
            normalizedNumber = GetSavedPayoutAddress(user, normalizedAssetCode);
        }
        else if (!TryNormalizePayoutAddress(address, normalizedAssetCode, out normalizedNumber, out var addressError))
        {
            return new WalletWithdrawRequestResult(false, 0m, addressError ?? "Please enter a valid payout address.");
        }

        if (normalizedNumber is null)
            return new WalletWithdrawRequestResult(false, 0m, "Please enter a valid payout address.");

        if (user.Balance < normalizedAmount)
            return new WalletWithdrawRequestResult(false, user.Balance, "Insufficient balance.", SavedAddresses: BuildSavedAddresses(user));

        var serverWallet = await EnsureServerWalletAsync(ct);
        var now = DateTimeOffset.UtcNow;
        user.Balance = RoundAmount(user.Balance - normalizedAmount);
        if (saveAddress)
            SetSavedPayoutAddress(user, normalizedAssetCode, normalizedNumber);

        var request = new WithdrawalRequest
        {
            UserId = user.Id,
            Amount = normalizedAmount,
            AssetCode = normalizedAssetCode,
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
        return new WalletWithdrawRequestResult(true, user.Balance, null, request, BuildSavedAddresses(user));
    }

    public async Task<WalletSaveAddressResult> SaveWalletAddressAsync(long userId, string assetCode, string address, CancellationToken ct)
    {
        await EnsureTonWithdrawalSchemaAsync(ct);

        var normalizedAssetCode = WithdrawalAssetCodes.Normalize(assetCode, defaultToBitcoin: false);
        if (normalizedAssetCode is null)
            return new WalletSaveAddressResult(false, "Unsupported withdrawal asset.");

        if (!TryNormalizePayoutAddress(address, normalizedAssetCode, out var normalizedAddress, out var addressError)
            || normalizedAddress is null)
        {
            return new WalletSaveAddressResult(false, addressError ?? "Please enter a valid wallet address.");
        }

        var user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null)
            return new WalletSaveAddressResult(false, "User was not found.");

        SetSavedPayoutAddress(user, normalizedAssetCode, normalizedAddress);
        await _db.SaveChangesAsync(ct);
        return new WalletSaveAddressResult(true, null, BuildSavedAddresses(user), normalizedAddress);
    }

    public async Task<WalletSavedAddresses> GetWalletAddressesAsync(long userId, CancellationToken ct)
    {
        await EnsureTonWithdrawalSchemaAsync(ct);

        var addresses = await _db.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new WalletSavedAddresses(x.WalletAddress, x.TonWalletAddress))
            .SingleOrDefaultAsync(ct);

        return addresses ?? new WalletSavedAddresses(null, null);
    }

    public async Task<IReadOnlyList<WalletHistoryEntry>> GetHistoryAsync(long userId, int limit, CancellationToken ct)
    {
        await EnsureTonWithdrawalSchemaAsync(ct);

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
                x.LastProviderEventType,
                x.PaymentMethod,
                x.AssetCode,
                x.AssetAmount))
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
                x.PayoutLastError ?? x.ReviewNote,
                GetHistoryPaymentMethod(x.AssetCode),
                x.AssetCode,
                x.AssetAmount))
            .ToListAsync(ct);

        return deposits
            .Concat(withdrawals)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToArray();
    }

    public async Task<WalletReviewWithdrawalResult> ConfirmWithdrawalAsync(long withdrawalRequestId, string adminUsername, string? payoutReference, CancellationToken ct)
    {
        await EnsureTonWithdrawalSchemaAsync(ct);

        var request = await _db.WithdrawalRequests
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == withdrawalRequestId, ct);

        if (request is null)
            return new WalletReviewWithdrawalResult(false, "Withdrawal request was not found.");

        if (request.Status != WithdrawalRequestStatus.Pending)
            return new WalletReviewWithdrawalResult(false, "Only pending requests can be confirmed.");

        if (!string.IsNullOrWhiteSpace(request.ExternalPayoutId))
            return new WalletReviewWithdrawalResult(false, "This withdrawal already has an external payout id.");

        var normalizedAssetCode = WithdrawalAssetCodes.Normalize(request.AssetCode, defaultToBitcoin: true)
            ?? WithdrawalAssetCodes.Bitcoin;

        var serverWallet = await EnsureServerWalletAsync(ct);
        if (serverWallet.Balance < request.Amount)
            return new WalletReviewWithdrawalResult(false, "Server wallet does not have enough balance to confirm this withdrawal.");

        var now = DateTimeOffset.UtcNow;

        if (normalizedAssetCode == WithdrawalAssetCodes.Bitcoin)
        {
            if (!_payments.Enabled)
                return new WalletReviewWithdrawalResult(false, "Payments are disabled. Enable BTCPay payouts before confirming withdrawals.");

            var payoutResult = await _btcPay.CreatePayoutAsync(new BtcPayCreatePayoutRequest(
                request.Amount,
                _payments.BtcPay.DefaultCurrency,
                request.Number,
                Reference: $"withdrawal:{request.Id}"), ct);
            if (!payoutResult.Success)
                return new WalletReviewWithdrawalResult(false, payoutResult.Error ?? "Failed to create BTCPay payout.");

            request.ExternalPayoutId = payoutResult.PayoutId;
            request.ExternalPayoutState = payoutResult.State;
            request.ExternalPayoutCreatedAtUtc = payoutResult.CreatedAtUtc ?? now;
        }
        else
        {
            if (!_payments.Enabled || !_payments.TelegramTon.Enabled || !_payments.TelegramTon.ServerWithdrawalsEnabled)
            {
                return new WalletReviewWithdrawalResult(false, "Server-executed TON withdrawals are disabled.");
            }

            var resolvedRate = await _telegramTonRate.GetResolvedRateAsync(ct);
            var usdPerTon = resolvedRate?.UsdPerTon ?? 0m;
            if (usdPerTon <= 0m)
                return new WalletReviewWithdrawalResult(false, "TON rate is unavailable right now.");

            var tonAmount = RoundAssetAmountUp(request.Amount / usdPerTon, 9);
            if (tonAmount <= 0m)
                return new WalletReviewWithdrawalResult(false, "Calculated TON payout amount is too small.");

            request.AssetAmount = tonAmount;
            request.AssetRate = usdPerTon;
            request.PayoutMemo ??= BuildTonWithdrawalMemo(request.Id);
            request.ExternalPayoutId = null;
            request.ExternalPayoutState = TonWithdrawalPayoutStates.Queued;
            request.ExternalPayoutCreatedAtUtc = now;
            request.PayoutAttemptCount = 0;
            request.PayoutSeqno = null;
            request.PayoutLastAttemptAtUtc = null;
            request.PayoutSubmittedAtUtc = null;
            request.PayoutConfirmedAtUtc = null;
            request.PayoutLastError = null;
        }

        serverWallet.Balance = RoundAmount(serverWallet.Balance - request.Amount);
        serverWallet.UpdatedAtUtc = now;

        request.Status = WithdrawalRequestStatus.Confirmed;
        request.AssetCode = normalizedAssetCode;
        request.ReviewedByAdmin = string.IsNullOrWhiteSpace(adminUsername) ? "admin" : adminUsername.Trim();
        request.ReviewNote = null;
        request.ReviewedAtUtc = now;

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

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateExternalPayoutId(ex))
        {
            return new WalletReviewWithdrawalResult(false, "This payout reference is already attached to another withdrawal request.");
        }

        return new WalletReviewWithdrawalResult(true, null);
    }

    public async Task<WalletReviewWithdrawalResult> DenyWithdrawalAsync(long withdrawalRequestId, string adminUsername, string? note, CancellationToken ct)
    {
        await EnsureTonWithdrawalSchemaAsync(ct);

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

    private static bool TryNormalizePayoutAddress(string? value, string assetCode, out string? normalizedAddress, out string? error)
    {
        normalizedAddress = null;
        error = null;

        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        if (trimmed.Length > 256)
            return false;

        var normalizedAssetCode = WithdrawalAssetCodes.Normalize(assetCode, defaultToBitcoin: true) ?? WithdrawalAssetCodes.Bitcoin;
        if (normalizedAssetCode == WithdrawalAssetCodes.Ton)
        {
            try
            {
                var tonAddress = new Address(trimmed);
                if (tonAddress.IsTestOnly())
                {
                    error = "wallet_ton_testnet_not_supported";
                    return false;
                }

                normalizedAddress = ToCanonicalTonAddress(tonAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }

        normalizedAddress = trimmed;
        return true;
    }

    private async Task EnsureTonWithdrawalSchemaAsync(CancellationToken ct)
    {
        if (!_db.Database.IsRelational())
            return;

        var pendingMigrations = await _db.Database.GetPendingMigrationsAsync(ct);
        if (!pendingMigrations.Any(x => string.Equals(x, TonWithdrawalSupportMigrationName, StringComparison.OrdinalIgnoreCase)))
            return;

        await _db.Database.MigrateAsync(ct);
    }

    private static string? GetSavedPayoutAddress(MiniAppUser user, string assetCode)
    {
        var value = assetCode switch
        {
            WithdrawalAssetCodes.Ton => user.TonWalletAddress,
            _ => user.WalletAddress
        };

        return TryNormalizePayoutAddress(value, assetCode, out var normalizedAddress, out _)
            ? normalizedAddress
            : null;
    }

    private static void SetSavedPayoutAddress(MiniAppUser user, string assetCode, string address)
    {
        if (assetCode == WithdrawalAssetCodes.Ton)
        {
            user.TonWalletAddress = address;
            return;
        }

        user.WalletAddress = address;
    }

    private static WalletSavedAddresses BuildSavedAddresses(MiniAppUser user)
        => new(user.WalletAddress, user.TonWalletAddress);

    private static string? GetHistoryPaymentMethod(string? assetCode)
    {
        return WithdrawalAssetCodes.Normalize(assetCode, defaultToBitcoin: true) switch
        {
            WithdrawalAssetCodes.Ton => PaymentMethodKeys.TelegramTon,
            WithdrawalAssetCodes.Bitcoin => PaymentMethodKeys.BtcPayCrypto,
            _ => null
        };
    }

    private static string MapWithdrawalHistoryStatus(WithdrawalRequestStatus status, string? externalPayoutState)
    {
        var payoutState = (externalPayoutState ?? string.Empty).Trim().ToLowerInvariant();
        var isPaid = payoutState is "completed" or "sent" or "paid" or "confirmed" or "succeeded";
        var isRejected = payoutState is "failed" or "cancelled" or "rejected" or "expired" or "invalid";
        var isProcessing = payoutState is "queued" or "sending" or "submitted" or "retry_pending";

        return status switch
        {
            WithdrawalRequestStatus.Confirmed when isPaid => "paid",
            WithdrawalRequestStatus.Confirmed when isRejected => "rejected",
            WithdrawalRequestStatus.Confirmed when isProcessing => "processing",
            WithdrawalRequestStatus.Pending => "waiting_for_admin_approval",
            WithdrawalRequestStatus.Denied => "rejected",
            WithdrawalRequestStatus.Confirmed => "processing",
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

    private static string ToCanonicalTonAddress(Address address)
    {
        var options = new AddressStringifyOptions(true, true, false, 0)
        {
            UrlSafe = true,
            Bounceable = false,
            TestOnly = false,
            Workchain = null
        };

        return address.ToString(AddressType.Base64, options);
    }

    private static string BuildTonWithdrawalMemo(long withdrawalRequestId)
        => $"WD-{withdrawalRequestId}";

    private static decimal RoundAssetAmountUp(decimal amount, int decimals)
    {
        var factor = 1m;
        for (var i = 0; i < decimals; i++)
            factor *= 10m;

        return Math.Ceiling(amount * factor) / factor;
    }

    private static bool IsDuplicateExternalPayoutId(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation
            && string.Equals(pg.ConstraintName, "IX_withdrawal_requests_ExternalPayoutId", StringComparison.Ordinal);
    }
}



