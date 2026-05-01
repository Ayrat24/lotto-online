using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MiniApp.Data;

namespace MiniApp.Features.Payments;

public enum TelegramTonWithdrawalManualProcessResultType
{
    Processed = 0,
    NoChange = 1,
    NotFound = 2,
    NotEligible = 3,
    Disabled = 4
}

public sealed record TelegramTonWithdrawalManualProcessResult(
    TelegramTonWithdrawalManualProcessResultType Type,
    string? DiagnosticMessage = null);

public sealed class TelegramTonWithdrawalProcessor
{
    private readonly AppDbContext _db;
    private readonly IWalletService _wallet;
    private readonly ITelegramTonHotWalletService _hotWallet;
    private readonly PaymentsOptions _options;
    private readonly ILogger<TelegramTonWithdrawalProcessor> _logger;

    public TelegramTonWithdrawalProcessor(
        AppDbContext db,
        IWalletService wallet,
        ITelegramTonHotWalletService hotWallet,
        IOptions<PaymentsOptions> options,
        ILogger<TelegramTonWithdrawalProcessor> logger)
    {
        _db = db;
        _wallet = wallet;
        _hotWallet = hotWallet;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ProcessNextAsync(CancellationToken ct)
    {
        if (!_options.Enabled || !_options.TelegramTon.Enabled || !_options.TelegramTon.ServerWithdrawalsEnabled)
            return 0;

        if (await RecoverStaleSendingWithdrawalAsync(ct))
            return 1;

        if (await SubmitNextQueuedWithdrawalAsync(ct))
            return 1;

        if (await ReconcileNextSubmittedWithdrawalAsync(ct))
            return 1;

        return 0;
    }

    public async Task<TelegramTonWithdrawalManualProcessResult> ProcessRequestAsync(long withdrawalRequestId, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.TelegramTon.Enabled || !_options.TelegramTon.ServerWithdrawalsEnabled)
        {
            return new TelegramTonWithdrawalManualProcessResult(
                TelegramTonWithdrawalManualProcessResultType.Disabled,
                "Server-executed TON withdrawals are disabled.");
        }

        var request = await _db.WithdrawalRequests
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == withdrawalRequestId, ct);
        if (request is null)
        {
            return new TelegramTonWithdrawalManualProcessResult(
                TelegramTonWithdrawalManualProcessResultType.NotFound,
                $"Withdrawal request {withdrawalRequestId} was not found.");
        }

        if (!string.Equals(request.AssetCode, WithdrawalAssetCodes.Ton, StringComparison.OrdinalIgnoreCase)
            || request.Status != WithdrawalRequestStatus.Confirmed)
        {
            return new TelegramTonWithdrawalManualProcessResult(
                TelegramTonWithdrawalManualProcessResultType.NotEligible,
                "Only confirmed TON withdrawals can be refreshed from history.");
        }

        var payoutState = NormalizePayoutState(request.ExternalPayoutState);
        var changed = payoutState switch
        {
            TonWithdrawalPayoutStates.Queued or TonWithdrawalPayoutStates.RetryPending
                => await SubmitQueuedWithdrawalAsync(request, ct),
            TonWithdrawalPayoutStates.Sending
                => await RefreshSendingWithdrawalAsync(request, ct),
            TonWithdrawalPayoutStates.Submitted
                => await ReconcileSubmittedWithdrawalAsync(request, ct),
            _ => false
        };

        return payoutState is TonWithdrawalPayoutStates.Queued or TonWithdrawalPayoutStates.RetryPending or TonWithdrawalPayoutStates.Sending or TonWithdrawalPayoutStates.Submitted
            ? new TelegramTonWithdrawalManualProcessResult(
                changed ? TelegramTonWithdrawalManualProcessResultType.Processed : TelegramTonWithdrawalManualProcessResultType.NoChange,
                changed ? $"Processed TON withdrawal request {withdrawalRequestId}." : $"No new TON withdrawal state change was detected for request {withdrawalRequestId}.")
            : new TelegramTonWithdrawalManualProcessResult(
                TelegramTonWithdrawalManualProcessResultType.NotEligible,
                $"TON withdrawal request {withdrawalRequestId} is not in a retryable state.");
    }

    private async Task<bool> RecoverStaleSendingWithdrawalAsync(CancellationToken ct)
    {
        var staleBeforeUtc = DateTimeOffset.UtcNow.AddMinutes(-2);
        var request = await _db.WithdrawalRequests
            .Include(x => x.User)
            .Where(x => x.AssetCode == WithdrawalAssetCodes.Ton
                && x.Status == WithdrawalRequestStatus.Confirmed
                && x.ExternalPayoutState == TonWithdrawalPayoutStates.Sending
                && x.PayoutLastAttemptAtUtc != null
                && x.PayoutLastAttemptAtUtc <= staleBeforeUtc)
            .OrderBy(x => x.PayoutLastAttemptAtUtc)
            .FirstOrDefaultAsync(ct);

        if (request is null)
            return false;

        return await RecoverSendingWithdrawalAsync(request, ct);
    }

    private async Task<bool> RecoverSendingWithdrawalAsync(WithdrawalRequest request, CancellationToken ct)
    {
        var walletState = await _hotWallet.GetHotWalletStateAsync(ct);
        if (walletState.Success
            && request.PayoutSeqno is { } reservedSeqno
            && walletState.Seqno is { } currentSeqno
            && currentSeqno > reservedSeqno)
        {
            request.ExternalPayoutState = TonWithdrawalPayoutStates.Submitted;
            request.PayoutSubmittedAtUtc ??= DateTimeOffset.UtcNow;
            request.PayoutLastError = "Recovered submitted TON payout after stale send state.";
            await _db.SaveChangesAsync(ct);
            return true;
        }

        if (request.PayoutAttemptCount >= _options.TelegramTon.WithdrawalMaxRetryAttempts)
        {
            await FailAndRefundAsync(request, "TON payout stayed in sending state and exceeded the retry limit. Funds were returned to the user.", ct);
            return true;
        }

        request.ExternalPayoutState = TonWithdrawalPayoutStates.RetryPending;
        request.PayoutLastError = "Recovered stale TON payout send state. The worker will retry broadcast.";
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> SubmitNextQueuedWithdrawalAsync(CancellationToken ct)
    {
        var request = await _db.WithdrawalRequests
            .Include(x => x.User)
            .Where(x => x.AssetCode == WithdrawalAssetCodes.Ton
                && x.Status == WithdrawalRequestStatus.Confirmed
                && (x.ExternalPayoutState == TonWithdrawalPayoutStates.Queued
                    || x.ExternalPayoutState == TonWithdrawalPayoutStates.RetryPending))
            .OrderBy(x => x.ExternalPayoutCreatedAtUtc ?? x.ReviewedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (request is null)
            return false;

        return await SubmitQueuedWithdrawalAsync(request, ct);
    }

    private async Task<bool> SubmitQueuedWithdrawalAsync(WithdrawalRequest request, CancellationToken ct)
    {
        if (request.AssetAmount is null || request.AssetAmount <= 0m || string.IsNullOrWhiteSpace(request.PayoutMemo))
        {
            await FailAndRefundAsync(request, "TON payout details are incomplete. Funds were returned to the user.", ct);
            return true;
        }

        var walletState = await _hotWallet.GetHotWalletStateAsync(ct);
        if (!walletState.Success || walletState.Seqno is null)
        {
            await HandleRetryableFailureAsync(request, walletState.Error ?? "Failed to query TON hot wallet state.", ct);
            return true;
        }

        if (!walletState.IsDeployed)
        {
            await HandleRetryableFailureAsync(request, "TON hot wallet is not deployed on-chain.", ct);
            return true;
        }

        request.ExternalPayoutState = TonWithdrawalPayoutStates.Sending;
        request.PayoutAttemptCount += 1;
        request.PayoutSeqno = walletState.Seqno;
        request.PayoutLastAttemptAtUtc = DateTimeOffset.UtcNow;
        request.PayoutLastError = null;
        await _db.SaveChangesAsync(ct);

        var sendResult = await _hotWallet.SendWithdrawalAsync(new TelegramTonSendWithdrawalRequest(
            request.Number,
            request.AssetAmount.Value,
            request.PayoutMemo!,
            request.PayoutSeqno.Value,
            Bounce: ShouldBounceTonWithdrawal(request.Number),
            ValidForSeconds: _options.TelegramTon.WithdrawalMessageTtlSeconds), ct);

        if (!sendResult.Success)
        {
            await HandleRetryableFailureAsync(request, sendResult.Error ?? "TON payout broadcast failed.", ct);
            return true;
        }

        request.ExternalPayoutState = TonWithdrawalPayoutStates.Submitted;
        request.PayoutSubmittedAtUtc = sendResult.SubmittedAtUtc ?? DateTimeOffset.UtcNow;
        request.PayoutLastError = null;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Submitted TON withdrawal request {WithdrawalRequestId} with reserved seqno {Seqno} and memo {Memo}.",
            request.Id,
            request.PayoutSeqno,
            request.PayoutMemo);

        return true;
    }

    private async Task<bool> ReconcileNextSubmittedWithdrawalAsync(CancellationToken ct)
    {
        var request = await _db.WithdrawalRequests
            .Include(x => x.User)
            .Where(x => x.AssetCode == WithdrawalAssetCodes.Ton
                && x.Status == WithdrawalRequestStatus.Confirmed
                && (x.ExternalPayoutState == TonWithdrawalPayoutStates.Submitted
                    || x.ExternalPayoutState == TonWithdrawalPayoutStates.Sending))
            .OrderBy(x => x.PayoutSubmittedAtUtc ?? x.PayoutLastAttemptAtUtc ?? x.ExternalPayoutCreatedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (request is null || request.AssetAmount is null || string.IsNullOrWhiteSpace(request.PayoutMemo))
            return false;

        return await ReconcileSubmittedWithdrawalAsync(request, ct);
    }

    private async Task<bool> RefreshSendingWithdrawalAsync(WithdrawalRequest request, CancellationToken ct)
    {
        var staleBeforeUtc = DateTimeOffset.UtcNow.AddMinutes(-2);
        if (request.PayoutLastAttemptAtUtc != null && request.PayoutLastAttemptAtUtc <= staleBeforeUtc)
            return await RecoverSendingWithdrawalAsync(request, ct);

        return await ReconcileSubmittedWithdrawalAsync(request, ct);
    }

    private async Task<bool> ReconcileSubmittedWithdrawalAsync(WithdrawalRequest request, CancellationToken ct)
    {
        if (request.AssetAmount is null || string.IsNullOrWhiteSpace(request.PayoutMemo))
            return false;

        var lookup = await _hotWallet.TryFindOutgoingTransferAsync(new TelegramTonOutgoingTransferLookupRequest(
            request.Number,
            request.AssetAmount.Value,
            request.PayoutMemo!,
            request.PayoutLastAttemptAtUtc ?? request.ExternalPayoutCreatedAtUtc ?? request.CreatedAtUtc,
            Math.Max(_options.TelegramTon.TransactionSearchLimit, 25)), ct);

        if (lookup.Success && lookup.TransferFound)
        {
            request.ExternalPayoutId = lookup.TransactionHash;
            request.ExternalPayoutState = TonWithdrawalPayoutStates.Confirmed;
            request.PayoutConfirmedAtUtc = lookup.ObservedAtUtc ?? DateTimeOffset.UtcNow;
            request.PayoutSubmittedAtUtc ??= lookup.ObservedAtUtc ?? DateTimeOffset.UtcNow;
            request.PayoutLastError = null;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Confirmed TON withdrawal request {WithdrawalRequestId} on-chain with transaction {TransactionHash}.",
                request.Id,
                request.ExternalPayoutId);

            return true;
        }

        if (!lookup.Success)
        {
            request.PayoutLastError = TruncateError(lookup.Error ?? "TON payout lookup failed.");
            await _db.SaveChangesAsync(ct);
            return true;
        }

        var timeoutUtc = (request.PayoutSubmittedAtUtc ?? request.PayoutLastAttemptAtUtc ?? request.ExternalPayoutCreatedAtUtc ?? request.CreatedAtUtc)
            .AddMinutes(Math.Max(_options.TelegramTon.WithdrawalConfirmationTimeoutMinutes, 1));
        if (DateTimeOffset.UtcNow < timeoutUtc)
            return false;

        var walletState = await _hotWallet.GetHotWalletStateAsync(ct);
        if (walletState.Success
            && request.PayoutSeqno is { } reservedSeqno
            && walletState.Seqno is { } currentSeqno)
        {
            if (currentSeqno <= reservedSeqno)
            {
                if (request.PayoutAttemptCount >= _options.TelegramTon.WithdrawalMaxRetryAttempts)
                {
                    await FailAndRefundAsync(request, "TON payout was not accepted on-chain after multiple attempts. Funds were returned to the user.", ct);
                    return true;
                }

                request.ExternalPayoutState = TonWithdrawalPayoutStates.RetryPending;
                request.PayoutLastError = "TON payout confirmation timed out before the reserved seqno advanced. The worker will retry broadcast.";
                await _db.SaveChangesAsync(ct);
                return true;
            }

            request.PayoutLastError = "TON wallet seqno advanced, but the matching on-chain payout transaction has not been located yet. Waiting for additional reconciliation.";
            await _db.SaveChangesAsync(ct);
            return true;
        }

        request.PayoutLastError = TruncateError(walletState.Error ?? "TON payout confirmation timed out.");
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task HandleRetryableFailureAsync(WithdrawalRequest request, string error, CancellationToken ct)
    {
        request.PayoutLastError = TruncateError(error);

        if (request.PayoutAttemptCount >= _options.TelegramTon.WithdrawalMaxRetryAttempts)
        {
            await FailAndRefundAsync(request, "TON payout failed after multiple attempts. Funds were returned to the user.", ct);
            return;
        }

        request.ExternalPayoutState = TonWithdrawalPayoutStates.RetryPending;
        await _db.SaveChangesAsync(ct);
    }

    private async Task FailAndRefundAsync(WithdrawalRequest request, string reason, CancellationToken ct)
    {
        if (request.Status == WithdrawalRequestStatus.Denied)
            return;

        var now = DateTimeOffset.UtcNow;
        var reference = $"withdrawal-failed:{request.Id}";
        var alreadyRefunded = await _db.WalletTransactions
            .AsNoTracking()
            .AnyAsync(x => x.Type == WalletTransactionType.WithdrawalDeniedRefund && x.Reference == reference, ct);
        if (alreadyRefunded)
        {
            request.Status = WithdrawalRequestStatus.Denied;
            request.ExternalPayoutState = TonWithdrawalPayoutStates.Failed;
            request.PayoutLastError = TruncateError(reason);
            request.ReviewNote = reason;
            request.ReviewedAtUtc ??= now;
            await _db.SaveChangesAsync(ct);
            return;
        }

        var serverWallet = await _wallet.EnsureServerWalletAsync(ct);
        request.User.Balance = RoundAmount(request.User.Balance + request.Amount);
        serverWallet.Balance = RoundAmount(serverWallet.Balance + request.Amount);
        serverWallet.UpdatedAtUtc = now;

        request.Status = WithdrawalRequestStatus.Denied;
        request.ExternalPayoutState = TonWithdrawalPayoutStates.Failed;
        request.PayoutLastError = TruncateError(reason);
        request.ReviewNote = reason;
        request.ReviewedAtUtc ??= now;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = request.UserId,
            Type = WalletTransactionType.WithdrawalDeniedRefund,
            UserDelta = request.Amount,
            UserBalanceAfter = request.User.Balance,
            ServerDelta = request.Amount,
            ServerBalanceAfter = serverWallet.Balance,
            Reference = reference,
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync(ct);
    }

    private static bool ShouldBounceTonWithdrawal(string address)
    {
        try
        {
            return new TonSdk.Core.Address(address.Trim()).IsBounceable();
        }
        catch
        {
            return false;
        }
    }

    private static decimal RoundAmount(decimal amount)
        => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static string TruncateError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "Unhandled TON payout error.";

        return error.Length <= 512 ? error : error[..512];
    }

    private static string NormalizePayoutState(string? payoutState)
        => (payoutState ?? string.Empty).Trim().ToLowerInvariant();
}



