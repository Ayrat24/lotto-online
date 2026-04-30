using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniApp.Data;
using Npgsql;

namespace MiniApp.Features.Payments;

public sealed class PaymentsService : IPaymentsService
{
    private const string BtcPayProviderName = "BTCPay";
    private const string TelegramTonProviderName = "TelegramTon";
    private const int TonDiagnosticsRecentTransferDisplayLimit = 8;
    private readonly AppDbContext _db;
    private readonly IWalletService _wallet;
    private readonly IReferralService _referrals;
    private readonly IBtcPayClient _btcPay;
    private readonly ITelegramTonClient _telegramTon;
    private readonly ITelegramTonRateService _telegramTonRate;
    private readonly ILogger<PaymentsService> _logger;
    private readonly PaymentsOptions _options;

    public PaymentsService(
        AppDbContext db,
        IWalletService wallet,
        IReferralService referrals,
        IBtcPayClient btcPay,
        ITelegramTonClient telegramTon,
        ITelegramTonRateService telegramTonRate,
        ILogger<PaymentsService> logger,
        IOptions<PaymentsOptions> options)
    {
        _db = db;
        _wallet = wallet;
        _referrals = referrals;
        _btcPay = btcPay;
        _telegramTon = telegramTon;
        _telegramTonRate = telegramTonRate;
        _logger = logger;
        _options = options.Value;
    }

    public PaymentSystemsView GetPaymentSystems()
    {
        var systems = new List<PaymentSystemView>();

        if (_options.Enabled && _options.TelegramTon.Enabled)
        {
            systems.Add(new PaymentSystemView(
                PaymentMethodKeys.TelegramTon,
                TelegramTonProviderName,
                "native_wallet",
                SupportsNativeTelegram: true,
                SupportsAlternativeLink: true,
                AssetCode: "TON",
                Network: "TON"));
        }

        if (_options.Enabled && _options.BtcPay.Enabled)
        {
            systems.Add(new PaymentSystemView(
                PaymentMethodKeys.BtcPayCrypto,
                BtcPayProviderName,
                "external_invoice",
                SupportsNativeTelegram: false,
                SupportsAlternativeLink: false,
                AssetCode: null,
                Network: null));
        }

        var defaultPaymentMethod = NormalizePaymentMethod(_options.DefaultPaymentMethod);
        if (!systems.Any(x => string.Equals(x.Key, defaultPaymentMethod, StringComparison.Ordinal)))
            defaultPaymentMethod = systems.FirstOrDefault()?.Key;

        var twaReturnUrl = string.IsNullOrWhiteSpace(_options.TelegramTon.TwaReturnUrl)
            ? null
            : _options.TelegramTon.TwaReturnUrl.Trim();

        return new PaymentSystemsView(_options.Enabled, defaultPaymentMethod, systems, twaReturnUrl);
    }

    public async Task<CreateCryptoDepositResult> CreateCryptoDepositAsync(long userId, decimal amount, string? currency, string? paymentMethod, CancellationToken ct)
    {
        if (!_options.Enabled)
            return new CreateCryptoDepositResult(false, "Crypto payments are disabled.");

        var normalizedAmount = RoundAmount(amount);
        if (normalizedAmount <= 0m)
            return new CreateCryptoDepositResult(false, "Amount must be greater than zero.");

        if (normalizedAmount > 100000m)
            return new CreateCryptoDepositResult(false, "Amount is too large.");

        var normalizedPaymentMethod = ResolvePaymentMethod(paymentMethod);
        if (normalizedPaymentMethod is null)
            return new CreateCryptoDepositResult(false, "Payment method is not available.");

        return normalizedPaymentMethod switch
        {
            PaymentMethodKeys.TelegramTon => await CreateTelegramTonDepositAsync(userId, normalizedAmount, currency, ct),
            PaymentMethodKeys.BtcPayCrypto => await CreateBtcPayDepositAsync(userId, normalizedAmount, currency, ct),
            _ => new CreateCryptoDepositResult(false, "Payment method is not supported.")
        };
    }

    public async Task<CryptoDepositStatusResult> GetCryptoDepositStatusAsync(long userId, long depositId, CancellationToken ct)
    {
        var deposit = await _db.CryptoDepositIntents
            .SingleOrDefaultAsync(x => x.Id == depositId && x.UserId == userId, ct);

        if (deposit is null)
            return new CryptoDepositStatusResult(false, "Deposit was not found.");

        if (ShouldRefreshDepositOnStatusRequest(deposit))
        {
            var now = DateTimeOffset.UtcNow;
            await RefreshDepositStatusAsync(deposit, now, ct);
            if (_db.ChangeTracker.HasChanges())
                await _db.SaveChangesAsync(ct);
        }

        return new CryptoDepositStatusResult(true, null, ToView(deposit));
    }

    public async Task<TelegramTonAdminDepositDiagnosticsResult> GetTelegramTonAdminDepositDiagnosticsAsync(int limit, CancellationToken ct)
    {
        var take = Math.Clamp(limit, 1, 100);
        var deposits = await _db.CryptoDepositIntents
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.PaymentMethod == PaymentMethodKeys.TelegramTon)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(ct);

        var diagnostics = new List<TelegramTonAdminDepositDiagnosticView>(deposits.Count);
        foreach (var deposit in deposits)
            diagnostics.Add(await BuildTelegramTonAdminDepositDiagnosticAsync(deposit, includeLookup: false, ct));

        return new TelegramTonAdminDepositDiagnosticsResult(true, null, diagnostics);
    }

    public async Task<TelegramTonAdminDepositReconcileResult> ReconcileTelegramTonAdminDepositAsync(long depositId, CancellationToken ct)
    {
        var deposit = await _db.CryptoDepositIntents
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == depositId && x.PaymentMethod == PaymentMethodKeys.TelegramTon, ct);

        if (deposit is null)
            return new TelegramTonAdminDepositReconcileResult(false, "TON deposit was not found.");

        var beforeStatus = deposit.Status;
        var beforeUpdatedAt = deposit.UpdatedAtUtc;
        var beforeCreditedAt = deposit.CreditedAtUtc;
        var now = DateTimeOffset.UtcNow;

        await RefreshTelegramTonDepositAsync(deposit, now, ct);
        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(ct);

        var changed = deposit.Status != beforeStatus
            || deposit.UpdatedAtUtc != beforeUpdatedAt
            || deposit.CreditedAtUtc != beforeCreditedAt;

        var diagnostic = await BuildTelegramTonAdminDepositDiagnosticAsync(deposit, includeLookup: true, ct);
        return new TelegramTonAdminDepositReconcileResult(true, null, diagnostic, changed);
    }

    public async Task<int> ReconcilePendingTelegramTonDepositsAsync(CancellationToken ct)
    {
        if (!_options.Enabled || !_options.TelegramTon.Enabled)
            return 0;

        var openStatuses = new[]
        {
            CryptoDepositStatus.AwaitingPayment,
            CryptoDepositStatus.Paid,
            CryptoDepositStatus.Confirmed
        };

        var deposits = await _db.CryptoDepositIntents
            .Where(x => x.PaymentMethod == PaymentMethodKeys.TelegramTon && openStatuses.Contains(x.Status))
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        if (deposits.Count == 0)
            return 0;

        var changedCount = 0;
        foreach (var deposit in deposits)
        {
            var beforeStatus = deposit.Status;
            var beforeUpdatedAt = deposit.UpdatedAtUtc;
            var beforeCreditedAt = deposit.CreditedAtUtc;

            await RefreshTelegramTonDepositAsync(deposit, DateTimeOffset.UtcNow, ct);

            if (deposit.Status != beforeStatus
                || deposit.UpdatedAtUtc != beforeUpdatedAt
                || deposit.CreditedAtUtc != beforeCreditedAt)
            {
                changedCount++;
            }
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(ct);

        return changedCount;
    }

    public async Task<ProcessWebhookResult> ProcessBtcPayWebhookAsync(string payloadJson, string? deliveryId, string? signature, CancellationToken ct)
    {
        if (!_options.Enabled || !_options.BtcPay.Enabled)
            return new ProcessWebhookResult(true, null);

        if (!ValidateSignatureIfConfigured(payloadJson, signature))
            return new ProcessWebhookResult(false, "Invalid BTCPay webhook signature.");

        if (!TryExtractEventData(payloadJson, out var eventType, out var providerObjectId))
            return new ProcessWebhookResult(false, "Invalid BTCPay webhook payload.");

        var normalizedDeliveryId = string.IsNullOrWhiteSpace(deliveryId) ? null : deliveryId.Trim();

        var now = DateTimeOffset.UtcNow;
        var evt = new PaymentWebhookEvent
        {
            Provider = BtcPayProviderName,
            DeliveryId = normalizedDeliveryId,
            EventType = eventType,
            ProviderObjectId = providerObjectId,
            PayloadJson = payloadJson,
            Status = PaymentWebhookEventStatus.Received,
            ReceivedAtUtc = now
        };

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (normalizedDeliveryId is null)
            {
                var payloadDuplicate = await _db.PaymentWebhookEvents
                    .AsNoTracking()
                    .AnyAsync(x => x.Provider == BtcPayProviderName
                        && x.ProviderObjectId == providerObjectId
                        && x.EventType == eventType
                        && x.PayloadJson == payloadJson, ct);

                if (payloadDuplicate)
                {
                    await tx.RollbackAsync(ct);
                    return new ProcessWebhookResult(true, null, Duplicate: true);
                }
            }

            _db.PaymentWebhookEvents.Add(evt);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (normalizedDeliveryId is not null && IsDuplicateDeliveryId(ex))
        {
            await tx.RollbackAsync(ct);
            return new ProcessWebhookResult(true, null, Duplicate: true);
        }

        try
        {
            var handled = IsPayoutEvent(eventType)
                ? await ApplyWithdrawalStatusFromWebhookAsync(providerObjectId!, eventType, payloadJson, now, ct)
                : await ApplyDepositStatusFromWebhookAsync(providerObjectId!, eventType, now, ct);

            if (!handled)
            {
                evt.Status = PaymentWebhookEventStatus.Ignored;
                evt.Error = IsPayoutEvent(eventType)
                    ? "No matching local withdrawal request."
                    : "No matching local deposit intent.";
                evt.ProcessedAtUtc = now;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return new ProcessWebhookResult(true, null);
            }

            evt.Status = PaymentWebhookEventStatus.Processed;
            evt.ProcessedAtUtc = now;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new ProcessWebhookResult(true, null);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);

            var failedEvent = new PaymentWebhookEvent
            {
                Provider = BtcPayProviderName,
                DeliveryId = normalizedDeliveryId,
                EventType = eventType,
                ProviderObjectId = providerObjectId,
                PayloadJson = payloadJson,
                Status = PaymentWebhookEventStatus.Failed,
                Error = TruncateError(ex.Message),
                ReceivedAtUtc = now,
                ProcessedAtUtc = DateTimeOffset.UtcNow
            };

            try
            {
                _db.PaymentWebhookEvents.Add(failedEvent);
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException) when (normalizedDeliveryId is not null)
            {
                // Another worker may have persisted the same delivery id while this attempt failed.
            }

            return new ProcessWebhookResult(false, "Failed to process webhook.");
        }
    }

    private async Task<CreateCryptoDepositResult> CreateBtcPayDepositAsync(long userId, decimal amount, string? currency, CancellationToken ct)
    {
        if (!_options.BtcPay.Enabled)
            return new CreateCryptoDepositResult(false, "BTCPay deposits are disabled.");

        var normalizedCurrency = NormalizeCurrency(currency) ?? _options.BtcPay.DefaultCurrency;
        if (string.IsNullOrWhiteSpace(normalizedCurrency))
            normalizedCurrency = "USD";

        var orderId = $"u{userId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";
        var invoice = await _btcPay.CreateInvoiceAsync(amount, normalizedCurrency, orderId, ct);
        if (!invoice.Success)
            return new CreateCryptoDepositResult(false, invoice.Error ?? "Failed to create invoice.");

        var now = DateTimeOffset.UtcNow;
        var deposit = new CryptoDepositIntent
        {
            UserId = userId,
            Amount = amount,
            Currency = normalizedCurrency,
            PaymentMethod = PaymentMethodKeys.BtcPayCrypto,
            AssetCode = normalizedCurrency,
            Provider = BtcPayProviderName,
            ProviderInvoiceId = invoice.InvoiceId!,
            CheckoutLink = invoice.CheckoutLink!,
            Status = CryptoDepositStatus.AwaitingPayment,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ExpiresAtUtc = invoice.ExpirationTimeUtc
        };

        _db.CryptoDepositIntents.Add(deposit);
        await _db.SaveChangesAsync(ct);

        return new CreateCryptoDepositResult(true, null, ToView(deposit));
    }

    private async Task<CreateCryptoDepositResult> CreateTelegramTonDepositAsync(long userId, decimal amount, string? currency, CancellationToken ct)
    {
        if (!_options.TelegramTon.Enabled)
            return new CreateCryptoDepositResult(false, "Telegram TON deposits are disabled.");

        var normalizedCurrency = NormalizeCurrency(currency) ?? "USD";
        if (!string.Equals(normalizedCurrency, "USD", StringComparison.Ordinal))
            return new CreateCryptoDepositResult(false, "Telegram TON deposits currently credit wallet balance in USD only.");

        var resolvedRate = await _telegramTonRate.GetResolvedRateAsync(ct);
        var usdPerTon = resolvedRate?.UsdPerTon ?? 0m;
        if (usdPerTon <= 0m)
            return new CreateCryptoDepositResult(false, "Telegram TON rate is not configured.");

        if (resolvedRate is not null)
        {
            _logger.LogInformation(
                "Using USD/TON rate {UsdPerTon} from {Source} (fallback={IsFallback}, stale={IsStale}, observedAt={ObservedAtUtc:u}) for Telegram TON deposit creation.",
                resolvedRate.UsdPerTon,
                resolvedRate.Source,
                resolvedRate.IsFallback,
                resolvedRate.IsStale,
                resolvedRate.ObservedAtUtc);
        }

        var tonAmount = RoundAssetAmountUp(amount / usdPerTon, 6);
        if (tonAmount <= 0m)
            return new CreateCryptoDepositResult(false, "Calculated TON amount is too small.");

        var merchantAddress = (_options.TelegramTon.MerchantAddress ?? string.Empty).Trim();
        if (merchantAddress.Length == 0)
            return new CreateCryptoDepositResult(false, "Telegram TON wallet address is not configured.");

        var reference = BuildTelegramTonReference(userId);
        var tonLink = BuildTonTransferLink(merchantAddress, tonAmount, reference);
        var alternativeLink = BuildTonkeeperTransferLink(merchantAddress, tonAmount, reference);
        var now = DateTimeOffset.UtcNow;
        var expiresAtUtc = now.AddMinutes(Math.Max(_options.TelegramTon.PaymentTimeoutMinutes, 1));

        var deposit = new CryptoDepositIntent
        {
            UserId = userId,
            Amount = amount,
            Currency = normalizedCurrency,
            PaymentMethod = PaymentMethodKeys.TelegramTon,
            AssetCode = "TON",
            AssetAmount = tonAmount,
            Network = "TON",
            Provider = TelegramTonProviderName,
            ProviderInvoiceId = reference,
            CheckoutLink = tonLink,
            AlternativeCheckoutLink = alternativeLink,
            DestinationAddress = merchantAddress,
            DestinationMemo = reference,
            Status = CryptoDepositStatus.AwaitingPayment,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc
        };

        _db.CryptoDepositIntents.Add(deposit);
        await _db.SaveChangesAsync(ct);

        return new CreateCryptoDepositResult(true, null, ToView(deposit));
    }

    private async Task RefreshDepositStatusAsync(CryptoDepositIntent deposit, DateTimeOffset now, CancellationToken ct)
    {
        if (IsTerminalDepositStatus(deposit.Status))
            return;

        if (string.Equals(deposit.PaymentMethod, PaymentMethodKeys.BtcPayCrypto, StringComparison.Ordinal))
        {
            await RefreshBtcPayDepositAsync(deposit, now, ct);
            return;
        }

        if (string.Equals(deposit.PaymentMethod, PaymentMethodKeys.TelegramTon, StringComparison.Ordinal))
        {
            await RefreshTelegramTonDepositAsync(deposit, now, ct);
        }
    }

    private async Task RefreshBtcPayDepositAsync(CryptoDepositIntent deposit, DateTimeOffset now, CancellationToken ct)
    {
        if (!_options.BtcPay.Enabled || IsTerminalDepositStatus(deposit.Status))
            return;

        if (deposit.ExpiresAtUtc is not null && deposit.ExpiresAtUtc <= now && deposit.Status == CryptoDepositStatus.AwaitingPayment)
        {
            deposit.Status = CryptoDepositStatus.Expired;
            deposit.LastProviderEventType = "expired";
            deposit.UpdatedAtUtc = now;
            return;
        }

        var invoice = await _btcPay.GetInvoiceAsync(deposit.ProviderInvoiceId, ct);
        if (!invoice.Success || string.IsNullOrWhiteSpace(invoice.Status))
            return;

        if (!string.IsNullOrWhiteSpace(invoice.CheckoutLink))
            deposit.CheckoutLink = invoice.CheckoutLink!;

        if (invoice.ExpirationTimeUtc is not null)
            deposit.ExpiresAtUtc = invoice.ExpirationTimeUtc;

        ApplyStatusFromSignal(deposit, invoice.Status, now);
        if (ShouldCreditBalance(invoice.Status))
            await CreditDepositOnceAsync(deposit, now, ct);
    }

    private async Task RefreshTelegramTonDepositAsync(CryptoDepositIntent deposit, DateTimeOffset now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deposit.DestinationAddress)
            || string.IsNullOrWhiteSpace(deposit.DestinationMemo)
            || deposit.AssetAmount is null
            || deposit.AssetAmount.Value <= 0m)
        {
            return;
        }

        var lookup = await _telegramTon.TryFindIncomingTransferAsync(new TelegramTonLookupRequest(
            deposit.DestinationAddress,
            deposit.DestinationMemo,
            deposit.AssetAmount.Value,
            deposit.CreatedAtUtc,
            Math.Max(_options.TelegramTon.TransactionSearchLimit, 1),
            _options.TelegramTon.ExplorerBaseUrl), ct);

        if (!lookup.Success)
        {
            _logger.LogWarning(
                "Telegram TON lookup failed for deposit {DepositId} (userId={UserId}, address={Address}, memo={Memo}, expectedTon={ExpectedTon}). Error: {Error}",
                deposit.Id,
                deposit.UserId,
                deposit.DestinationAddress,
                deposit.DestinationMemo,
                deposit.AssetAmount,
                lookup.Error ?? "Unknown error");
            return;
        }

        if (!lookup.TransferFound)
        {
            if (deposit.ExpiresAtUtc is not null && deposit.ExpiresAtUtc <= now)
            {
                deposit.Status = CryptoDepositStatus.Expired;
                deposit.LastProviderEventType = "expired";
                deposit.UpdatedAtUtc = now;
            }

            return;
        }

        deposit.ProviderTransactionId ??= lookup.TransactionId;
        deposit.LastProviderEventType = "telegram_ton.transfer_detected";
        deposit.Status = CryptoDepositStatus.Confirmed;
        deposit.PaidAtUtc ??= lookup.ObservedAtUtc ?? now;
        deposit.ConfirmedAtUtc ??= lookup.ObservedAtUtc ?? now;
        deposit.UpdatedAtUtc = now;

        _logger.LogInformation(
            "Detected Telegram TON transfer for deposit {DepositId} (userId={UserId}, tx={TransactionId}, amountTon={AmountTon}, observedAt={ObservedAtUtc:u}).",
            deposit.Id,
            deposit.UserId,
            lookup.TransactionId,
            lookup.ReceivedTonAmount,
            lookup.ObservedAtUtc ?? now);

        await CreditDepositOnceAsync(deposit, now, ct);
    }

    private async Task CreditDepositOnceAsync(CryptoDepositIntent deposit, DateTimeOffset now, CancellationToken ct)
    {
        var reference = BuildCreditReference(deposit.PaymentMethod, deposit.ProviderInvoiceId);
        var alreadyCredited = deposit.CreditedAtUtc is not null
            || await _db.WalletTransactions
                .AsNoTracking()
                .AnyAsync(x => x.Type == WalletTransactionType.CryptoDepositCredited && x.Reference == reference, ct);

        if (alreadyCredited)
        {
            deposit.Status = CryptoDepositStatus.Credited;
            deposit.CreditedAtUtc ??= now;
            deposit.UpdatedAtUtc = now;
            return;
        }

        var user = await _db.Users.SingleAsync(x => x.Id == deposit.UserId, ct);
        var serverWallet = await _wallet.EnsureServerWalletAsync(ct);

        user.Balance = RoundAmount(user.Balance + deposit.Amount);
        serverWallet.Balance = RoundAmount(serverWallet.Balance + deposit.Amount);
        serverWallet.UpdatedAtUtc = now;

        deposit.Status = CryptoDepositStatus.Credited;
        deposit.CreditedAtUtc = now;
        deposit.UpdatedAtUtc = now;

        _db.WalletTransactions.Add(new WalletTransaction
        {
            UserId = deposit.UserId,
            Type = WalletTransactionType.CryptoDepositCredited,
            UserDelta = deposit.Amount,
            UserBalanceAfter = user.Balance,
            ServerDelta = deposit.Amount,
            ServerBalanceAfter = serverWallet.Balance,
            Reference = reference,
            CreatedAtUtc = now
        });

        await _referrals.ApplyBonusesForDepositAsync(deposit, now, ct);
    }

    private async Task<bool> ApplyDepositStatusFromWebhookAsync(string providerObjectId, string? eventType, DateTimeOffset now, CancellationToken ct)
    {
        var deposit = await _db.CryptoDepositIntents
            .SingleOrDefaultAsync(x => x.Provider == BtcPayProviderName && x.ProviderInvoiceId == providerObjectId, ct);

        if (deposit is null)
            return false;

        ApplyStatusFromSignal(deposit, eventType, now);
        if (ShouldCreditBalance(eventType))
            await CreditDepositOnceAsync(deposit, now, ct);

        return true;
    }

    private async Task<bool> ApplyWithdrawalStatusFromWebhookAsync(string payoutId, string? eventType, string payloadJson, DateTimeOffset now, CancellationToken ct)
    {
        var request = await _db.WithdrawalRequests
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.ExternalPayoutId == payoutId, ct);
        if (request is null)
            return false;

        var payoutState = ExtractPayoutState(payloadJson, eventType);
        request.ExternalPayoutState = payoutState ?? request.ExternalPayoutState;

        if (IsFinalPaidPayoutState(payoutState))
        {
            request.Status = WithdrawalRequestStatus.Confirmed;
            request.ReviewedAtUtc ??= now;
            return true;
        }

        if (IsRejectedPayoutState(payoutState))
            await RefundWithdrawalAsync(request, now, ct);

        return true;
    }

    private async Task RefundWithdrawalAsync(WithdrawalRequest request, DateTimeOffset now, CancellationToken ct)
    {
        if (request.Status == WithdrawalRequestStatus.Denied)
            return;

        var reference = $"withdrawal-failed:{request.Id}";
        var alreadyRefunded = await _db.WalletTransactions
            .AsNoTracking()
            .AnyAsync(x => x.Type == WalletTransactionType.WithdrawalDeniedRefund && x.Reference == reference, ct);
        if (alreadyRefunded)
        {
            request.Status = WithdrawalRequestStatus.Denied;
            request.ReviewedAtUtc ??= now;
            return;
        }

        var serverWallet = await _wallet.EnsureServerWalletAsync(ct);
        request.User.Balance = RoundAmount(request.User.Balance + request.Amount);
        serverWallet.Balance = RoundAmount(serverWallet.Balance + request.Amount);
        serverWallet.UpdatedAtUtc = now;

        request.Status = WithdrawalRequestStatus.Denied;
        request.ReviewedAtUtc ??= now;
        request.ReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote)
            ? "BTCPay payout was not completed. Funds returned to user."
            : request.ReviewNote;

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
    }

    private static void ApplyStatusFromSignal(CryptoDepositIntent deposit, string? signal, DateTimeOffset now)
    {
        deposit.LastProviderEventType = signal;
        deposit.UpdatedAtUtc = now;

        var value = (signal ?? string.Empty).ToLowerInvariant();
        if (value.Contains("expired", StringComparison.Ordinal))
        {
            deposit.Status = CryptoDepositStatus.Expired;
            return;
        }

        if (value.Contains("invalid", StringComparison.Ordinal))
        {
            deposit.Status = CryptoDepositStatus.Invalid;
            return;
        }

        if (value.Contains("confirmed", StringComparison.Ordinal)
            || value.Contains("settled", StringComparison.Ordinal)
            || value.Contains("completed", StringComparison.Ordinal)
            || value.Contains("succeeded", StringComparison.Ordinal))
        {
            deposit.Status = CryptoDepositStatus.Confirmed;
            deposit.ConfirmedAtUtc ??= now;
            deposit.PaidAtUtc ??= now;
            return;
        }

        if (value.Contains("paid", StringComparison.Ordinal)
            || value.Contains("receivedpayment", StringComparison.Ordinal)
            || value.Contains("processing", StringComparison.Ordinal))
        {
            deposit.Status = CryptoDepositStatus.Paid;
            deposit.PaidAtUtc ??= now;
        }
    }

    private static bool ShouldCreditBalance(string? signal)
    {
        var value = (signal ?? string.Empty).ToLowerInvariant();
        return value.Contains("confirmed", StringComparison.Ordinal)
            || value.Contains("settled", StringComparison.Ordinal)
            || value.Contains("invoiceconfirmed", StringComparison.Ordinal)
            || value.Contains("completed", StringComparison.Ordinal)
            || value.Contains("succeeded", StringComparison.Ordinal);
    }

    private static bool IsPayoutEvent(string? eventType)
    {
        var type = (eventType ?? string.Empty).ToLowerInvariant();
        return type.Contains("payout", StringComparison.Ordinal);
    }

    private static bool IsFinalPaidPayoutState(string? payoutState)
    {
        var state = (payoutState ?? string.Empty).ToLowerInvariant();
        return state is "completed" or "sent" or "paid" or "succeeded";
    }

    private static bool IsRejectedPayoutState(string? payoutState)
    {
        var state = (payoutState ?? string.Empty).ToLowerInvariant();
        return state is "cancelled" or "failed" or "rejected" or "expired" or "invalid";
    }

    private static string? ExtractPayoutState(string payloadJson, string? eventType)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("state", out var stateProp) && stateProp.ValueKind == JsonValueKind.String)
                return stateProp.GetString();

            if (root.TryGetProperty("currentState", out var rootCurrentState) && rootCurrentState.ValueKind == JsonValueKind.String)
                return rootCurrentState.GetString();

            if (root.TryGetProperty("status", out var rootStatus) && rootStatus.ValueKind == JsonValueKind.String)
                return rootStatus.GetString();

            if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object)
            {
                if (dataProp.TryGetProperty("state", out var dataState) && dataState.ValueKind == JsonValueKind.String)
                    return dataState.GetString();

                if (dataProp.TryGetProperty("currentState", out var currentState) && currentState.ValueKind == JsonValueKind.String)
                    return currentState.GetString();

                if (dataProp.TryGetProperty("newState", out var newState) && newState.ValueKind == JsonValueKind.String)
                    return newState.GetString();

                if (dataProp.TryGetProperty("status", out var dataStatus) && dataStatus.ValueKind == JsonValueKind.String)
                    return dataStatus.GetString();
            }
        }
        catch
        {
            return eventType;
        }

        return eventType;
    }

    private bool ValidateSignatureIfConfigured(string payloadJson, string? signatureHeader)
    {
        var secret = _options.BtcPay.WebhookSecret?.Trim();
        if (string.IsNullOrWhiteSpace(secret))
            return true;

        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        var expectedHash = ComputeSha256(secret, payloadJson);
        foreach (var candidate in EnumerateSignatures(signatureHeader))
        {
            if (!TryParseSignature(candidate, out var actualHash))
                continue;

            if (CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
                return true;
        }

        return false;
    }

    private static bool TryExtractEventData(string payloadJson, out string? eventType, out string? providerObjectId)
    {
        eventType = null;
        providerObjectId = null;

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
                eventType = typeProp.GetString();

            if (root.TryGetProperty("invoiceId", out var invoiceProp) && invoiceProp.ValueKind == JsonValueKind.String)
                providerObjectId = invoiceProp.GetString();

            if (string.IsNullOrWhiteSpace(providerObjectId)
                && root.TryGetProperty("payoutId", out var payoutProp)
                && payoutProp.ValueKind == JsonValueKind.String)
            {
                providerObjectId = payoutProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(providerObjectId)
                && root.TryGetProperty("data", out var dataProp)
                && dataProp.ValueKind == JsonValueKind.Object)
            {
                if (dataProp.TryGetProperty("payoutId", out var payoutIdProp) && payoutIdProp.ValueKind == JsonValueKind.String)
                    providerObjectId = payoutIdProp.GetString();

                if (dataProp.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                    providerObjectId = idProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(providerObjectId)
                && root.TryGetProperty("id", out var rootIdProp)
                && rootIdProp.ValueKind == JsonValueKind.String)
            {
                providerObjectId = rootIdProp.GetString();
            }

            return !string.IsNullOrWhiteSpace(providerObjectId);
        }
        catch
        {
            return false;
        }
    }

    private string? ResolvePaymentMethod(string? paymentMethod)
    {
        var available = GetPaymentSystems();
        if (!available.Enabled || available.Systems.Count == 0)
            return null;

        var normalized = NormalizePaymentMethod(paymentMethod) ?? NormalizePaymentMethod(available.DefaultPaymentMethod);
        if (normalized is not null && available.Systems.Any(x => string.Equals(x.Key, normalized, StringComparison.Ordinal)))
            return normalized;

        return available.Systems[0].Key;
    }

    private static string? NormalizePaymentMethod(string? paymentMethod)
    {
        var normalized = (paymentMethod ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            PaymentMethodKeys.BtcPayCrypto => PaymentMethodKeys.BtcPayCrypto,
            PaymentMethodKeys.TelegramTon => PaymentMethodKeys.TelegramTon,
            _ => null
        };
    }

    private static string? NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return null;

        var normalized = currency.Trim().ToUpperInvariant();
        return normalized.Length is >= 3 and <= 16 ? normalized : null;
    }

    private static bool IsTerminalDepositStatus(CryptoDepositStatus status)
        => status is CryptoDepositStatus.Credited or CryptoDepositStatus.Expired or CryptoDepositStatus.Invalid;

    private static bool ShouldRefreshDepositOnStatusRequest(CryptoDepositIntent deposit)
        => string.Equals(deposit.PaymentMethod, PaymentMethodKeys.BtcPayCrypto, StringComparison.Ordinal)
            || string.Equals(deposit.PaymentMethod, PaymentMethodKeys.TelegramTon, StringComparison.Ordinal);

    private async Task<TelegramTonAdminDepositDiagnosticView> BuildTelegramTonAdminDepositDiagnosticAsync(CryptoDepositIntent deposit, bool includeLookup, CancellationToken ct)
    {
        var creditReference = BuildCreditReference(deposit.PaymentMethod, deposit.ProviderInvoiceId);
        var walletTransactionExists = await _db.WalletTransactions
            .AsNoTracking()
            .AnyAsync(x => x.Type == WalletTransactionType.CryptoDepositCredited && x.Reference == creditReference, ct);

        TelegramTonLookupResult? lookup = null;
        TelegramTonRecentTransfersResult? recentTransfers = null;
        var canInspectWalletTransfers = !string.IsNullOrWhiteSpace(deposit.DestinationAddress);

        if (canInspectWalletTransfers)
        {
            recentTransfers = await _telegramTon.GetRecentIncomingTransfersAsync(new TelegramTonRecentTransfersRequest(
                deposit.DestinationAddress!,
                Math.Clamp(Math.Min(_options.TelegramTon.TransactionSearchLimit, TonDiagnosticsRecentTransferDisplayLimit), 1, 100),
                _options.TelegramTon.ExplorerBaseUrl), ct);
        }

        if (includeLookup
            && !string.IsNullOrWhiteSpace(deposit.DestinationAddress)
            && !string.IsNullOrWhiteSpace(deposit.DestinationMemo)
            && deposit.AssetAmount is not null
            && deposit.AssetAmount.Value > 0m)
        {
            lookup = await _telegramTon.TryFindIncomingTransferAsync(new TelegramTonLookupRequest(
                deposit.DestinationAddress,
                deposit.DestinationMemo,
                deposit.AssetAmount.Value,
                deposit.CreatedAtUtc,
                Math.Max(_options.TelegramTon.TransactionSearchLimit, 1),
                _options.TelegramTon.ExplorerBaseUrl), ct);
        }

        var recentTransferViews = (recentTransfers?.Transfers ?? Array.Empty<TelegramTonIncomingTransferView>())
            .Take(TonDiagnosticsRecentTransferDisplayLimit)
            .Select(tx => new TelegramTonAdminIncomingTransferDiagnosticView(
                tx.TransactionId,
                tx.ReceivedTonAmount,
                tx.ObservedAtUtc,
                tx.ExplorerLink,
                tx.SenderAddress,
                tx.Memo,
                string.Equals(tx.Memo?.Trim(), deposit.DestinationMemo?.Trim(), StringComparison.Ordinal),
                IsTonAmountMatch(deposit.AssetAmount, tx.ReceivedTonAmount),
                tx.ObservedAtUtc is null || tx.ObservedAtUtc.Value >= deposit.CreatedAtUtc.AddMinutes(-3)))
            .ToArray();

        return new TelegramTonAdminDepositDiagnosticView(
            deposit.Id,
            deposit.UserId,
            deposit.User.TelegramUserId,
            deposit.Amount,
            deposit.Currency,
            deposit.AssetAmount,
            deposit.Status.ToString(),
            deposit.ProviderInvoiceId,
            deposit.DestinationAddress,
            deposit.DestinationMemo,
            deposit.ProviderTransactionId,
            deposit.LastProviderEventType,
            deposit.CreatedAtUtc,
            deposit.ExpiresAtUtc,
            deposit.PaidAtUtc,
            deposit.ConfirmedAtUtc,
            deposit.CreditedAtUtc,
            creditReference,
            walletTransactionExists,
            lookup?.Success ?? false,
            lookup?.TransferFound ?? false,
            lookup?.Error,
            lookup?.TransactionId,
            lookup?.ReceivedTonAmount,
            lookup?.ObservedAtUtc,
            lookup?.ExplorerLink,
            lookup?.SenderAddress,
            recentTransfers is { Success: false } ? recentTransfers.Error : null,
            recentTransferViews);
    }

    private bool IsTonAmountMatch(decimal? expectedTonAmount, decimal? receivedTonAmount)
    {
        if (expectedTonAmount is null || expectedTonAmount.Value <= 0m || receivedTonAmount is null || receivedTonAmount.Value <= 0m)
            return false;

        var toleranceTon = decimal.Clamp(
            _options.TelegramTon.DepositMatchToleranceTon,
            0m,
            TelegramTonOptions.MaxDepositMatchToleranceTon);

        return ToNanotons(receivedTonAmount.Value) + ToNanotons(toleranceTon) >= ToNanotons(expectedTonAmount.Value);
    }

    private static decimal RoundAmount(decimal amount)
        => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static decimal RoundAssetAmountUp(decimal amount, int decimals)
    {
        var factor = 1m;
        for (var i = 0; i < decimals; i++)
            factor *= 10m;

        return Math.Ceiling(amount * factor) / factor;
    }

    private static string BuildCreditReference(string paymentMethod, string providerInvoiceId)
        => $"{paymentMethod}:{providerInvoiceId}";

    private static string BuildTelegramTonReference(long userId)
        => $"TON-{userId}-{RandomNumberGenerator.GetHexString(4)}";

    private static string BuildTonTransferLink(string address, decimal tonAmount, string memo)
        => $"ton://transfer/{Uri.EscapeDataString(address)}?amount={ToNanotons(tonAmount).ToString(CultureInfo.InvariantCulture)}&text={Uri.EscapeDataString(memo)}";

    private static string BuildTonkeeperTransferLink(string address, decimal tonAmount, string memo)
        => $"https://app.tonkeeper.com/transfer/{Uri.EscapeDataString(address)}?amount={ToNanotons(tonAmount).ToString(CultureInfo.InvariantCulture)}&text={Uri.EscapeDataString(memo)}";

    private static decimal ToNanotons(decimal tonAmount)
        => decimal.Round(tonAmount * 1_000_000_000m, 0, MidpointRounding.AwayFromZero);

    private static bool IsDuplicateDeliveryId(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation
            && string.Equals(pg.ConstraintName, "IX_payment_webhook_events_Provider_DeliveryId", StringComparison.Ordinal);
    }

    private static string TruncateError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "Unhandled error.";

        return error.Length <= 512 ? error : error[..512];
    }

    private CryptoDepositView ToView(CryptoDepositIntent x)
        => new(
            x.Id,
            x.PaymentMethod,
            x.Provider,
            x.ProviderInvoiceId,
            x.Amount,
            x.Currency,
            x.AssetAmount,
            x.AssetCode,
            x.Network,
            x.Status.ToString(),
            x.CheckoutLink,
            x.AlternativeCheckoutLink,
            x.DestinationAddress,
            x.DestinationMemo,
            x.ProviderTransactionId,
            x.CreatedAtUtc,
            x.ExpiresAtUtc,
            x.PaidAtUtc,
            x.ConfirmedAtUtc,
            x.CreditedAtUtc);

    private static IEnumerable<string> EnumerateSignatures(string header)
    {
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            yield return part;
    }

    private static bool TryParseSignature(string value, out byte[] hashBytes)
    {
        hashBytes = Array.Empty<byte>();
        var token = value.Trim();
        if (token.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            token = token[7..];

        try
        {
            hashBytes = Convert.FromHexString(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeSha256(string key, string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(payloadBytes);
    }
}

