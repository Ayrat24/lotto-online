using System.Security.Cryptography;
using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniApp.Data;
using Npgsql;

namespace MiniApp.Features.Payments;

public sealed class PaymentsService : IPaymentsService
{
    private const string ProviderName = "BTCPay";
    private readonly AppDbContext _db;
    private readonly IWalletService _wallet;
    private readonly IBtcPayClient _btcPay;
    private readonly PaymentsOptions _options;

    public PaymentsService(AppDbContext db, IWalletService wallet, IBtcPayClient btcPay, IOptions<PaymentsOptions> options)
    {
        _db = db;
        _wallet = wallet;
        _btcPay = btcPay;
        _options = options.Value;
    }

    public async Task<CreateCryptoDepositResult> CreateCryptoDepositAsync(long userId, decimal amount, string? currency, CancellationToken ct)
    {
        if (!_options.Enabled)
            return new CreateCryptoDepositResult(false, "Crypto payments are disabled.");

        var normalizedAmount = RoundAmount(amount);
        if (normalizedAmount <= 0m)
            return new CreateCryptoDepositResult(false, "Amount must be greater than zero.");

        if (normalizedAmount > 100000m)
            return new CreateCryptoDepositResult(false, "Amount is too large.");

        var normalizedCurrency = NormalizeCurrency(currency) ?? _options.BtcPay.DefaultCurrency;
        if (string.IsNullOrWhiteSpace(normalizedCurrency))
            normalizedCurrency = "USD";

        var orderId = $"u{userId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}";
        var invoice = await _btcPay.CreateInvoiceAsync(normalizedAmount, normalizedCurrency, orderId, ct);
        if (!invoice.Success)
            return new CreateCryptoDepositResult(false, invoice.Error ?? "Failed to create invoice.");

        var now = DateTimeOffset.UtcNow;
        var deposit = new CryptoDepositIntent
        {
            UserId = userId,
            Amount = normalizedAmount,
            Currency = normalizedCurrency,
            Provider = ProviderName,
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

    public async Task<CryptoDepositStatusResult> GetCryptoDepositStatusAsync(long userId, long depositId, CancellationToken ct)
    {
        var deposit = await _db.CryptoDepositIntents
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == depositId && x.UserId == userId, ct);

        if (deposit is null)
            return new CryptoDepositStatusResult(false, "Deposit was not found.");

        return new CryptoDepositStatusResult(true, null, ToView(deposit));
    }

    public async Task<ProcessWebhookResult> ProcessBtcPayWebhookAsync(string payloadJson, string? deliveryId, string? signature, CancellationToken ct)
    {
        if (!_options.Enabled)
            return new ProcessWebhookResult(true, null);

        if (!ValidateSignatureIfConfigured(payloadJson, signature))
            return new ProcessWebhookResult(false, "Invalid BTCPay webhook signature.");

        if (!TryExtractEventData(payloadJson, out var eventType, out var providerObjectId))
            return new ProcessWebhookResult(false, "Invalid BTCPay webhook payload.");

        var normalizedDeliveryId = string.IsNullOrWhiteSpace(deliveryId) ? null : deliveryId.Trim();

        var now = DateTimeOffset.UtcNow;
        var evt = new PaymentWebhookEvent
        {
            Provider = ProviderName,
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
                    .AnyAsync(x => x.Provider == ProviderName
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
                Provider = ProviderName,
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

    private async Task CreditDepositOnceAsync(CryptoDepositIntent deposit, DateTimeOffset now, CancellationToken ct)
    {
        var reference = BuildCreditReference(deposit.ProviderInvoiceId);
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
    }

    private async Task<bool> ApplyDepositStatusFromWebhookAsync(string providerObjectId, string? eventType, DateTimeOffset now, CancellationToken ct)
    {
        var deposit = await _db.CryptoDepositIntents
            .SingleOrDefaultAsync(x => x.Provider == ProviderName && x.ProviderInvoiceId == providerObjectId, ct);

        if (deposit is null)
            return false;

        ApplyStatusFromEvent(deposit, eventType, now);
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
        {
            await RefundWithdrawalAsync(request, now, ct);
        }

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

    private static void ApplyStatusFromEvent(CryptoDepositIntent deposit, string? eventType, DateTimeOffset now)
    {
        deposit.LastProviderEventType = eventType;
        deposit.UpdatedAtUtc = now;

        var type = (eventType ?? string.Empty).ToLowerInvariant();
        if (type.Contains("expired", StringComparison.Ordinal))
        {
            deposit.Status = CryptoDepositStatus.Expired;
            return;
        }

        if (type.Contains("invalid", StringComparison.Ordinal))
        {
            deposit.Status = CryptoDepositStatus.Invalid;
            return;
        }

        if (type.Contains("confirmed", StringComparison.Ordinal) || type.Contains("settled", StringComparison.Ordinal))
        {
            deposit.Status = CryptoDepositStatus.Confirmed;
            deposit.ConfirmedAtUtc ??= now;
            deposit.PaidAtUtc ??= now;
            return;
        }

        if (type.Contains("paid", StringComparison.Ordinal) || type.Contains("receivedpayment", StringComparison.Ordinal))
        {
            deposit.Status = CryptoDepositStatus.Paid;
            deposit.PaidAtUtc ??= now;
        }
    }

    private static bool ShouldCreditBalance(string? eventType)
    {
        var type = (eventType ?? string.Empty).ToLowerInvariant();
        return type.Contains("confirmed", StringComparison.Ordinal)
            || type.Contains("settled", StringComparison.Ordinal)
            || type.Contains("invoiceconfirmed", StringComparison.Ordinal);
    }

    private static bool IsPayoutEvent(string? eventType)
    {
        var type = (eventType ?? string.Empty).ToLowerInvariant();
        return type.Contains("payout", StringComparison.Ordinal);
    }

    private static bool IsFinalPaidPayoutState(string? payoutState)
    {
        var state = (payoutState ?? string.Empty).ToLowerInvariant();
        return state is "completed" or "sent";
    }

    private static bool IsRejectedPayoutState(string? payoutState)
    {
        var state = (payoutState ?? string.Empty).ToLowerInvariant();
        return state is "cancelled" or "failed";
    }

    private static string? ExtractPayoutState(string payloadJson, string? eventType)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("state", out var stateProp) && stateProp.ValueKind == JsonValueKind.String)
                return stateProp.GetString();

            if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Object)
            {
                if (dataProp.TryGetProperty("state", out var dataState) && dataState.ValueKind == JsonValueKind.String)
                    return dataState.GetString();

                if (dataProp.TryGetProperty("currentState", out var currentState) && currentState.ValueKind == JsonValueKind.String)
                    return currentState.GetString();
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
                && root.TryGetProperty("id", out var rootIdProp)
                && rootIdProp.ValueKind == JsonValueKind.String)
            {
                providerObjectId = rootIdProp.GetString();
            }

            if (string.IsNullOrWhiteSpace(providerObjectId)
                && root.TryGetProperty("data", out var dataProp)
                && dataProp.ValueKind == JsonValueKind.Object)
            {
                if (dataProp.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                    providerObjectId = idProp.GetString();
            }

            return !string.IsNullOrWhiteSpace(providerObjectId);
        }
        catch
        {
            return false;
        }
    }

    private static string? NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return null;

        var normalized = currency.Trim().ToUpperInvariant();
        return normalized.Length is >= 3 and <= 16 ? normalized : null;
    }

    private static decimal RoundAmount(decimal amount)
        => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    private static string BuildCreditReference(string providerInvoiceId)
        => $"btcpay:{providerInvoiceId}";

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

    private static CryptoDepositView ToView(CryptoDepositIntent x)
        => new(
            x.Id,
            x.Provider,
            x.ProviderInvoiceId,
            x.Amount,
            x.Currency,
            x.Status.ToString(),
            x.CheckoutLink,
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

