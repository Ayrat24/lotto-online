namespace MiniApp.Features.Payments;

public sealed record CreateCryptoDepositRequest(string InitData, decimal Amount, string? Currency, string? PaymentMethod);

public sealed record PaymentSystemsView(bool Enabled, string? DefaultPaymentMethod, IReadOnlyList<PaymentSystemView> Systems);

public sealed record PaymentSystemView(
    string Key,
    string Provider,
    string Kind,
    bool SupportsNativeTelegram,
    bool SupportsAlternativeLink,
    string? AssetCode,
    string? Network);

public sealed record GetCryptoDepositStatusRequest(string InitData, long DepositId);

public sealed record CreateCryptoDepositResult(bool Success, string? Error, CryptoDepositView? Deposit = null);

public sealed record CryptoDepositStatusResult(bool Success, string? Error, CryptoDepositView? Deposit = null);

public sealed record ProcessWebhookResult(bool Success, string? Error, bool Duplicate = false);

public sealed record CryptoDepositView(
    long Id,
    string PaymentMethod,
    string Provider,
    string ProviderInvoiceId,
    decimal Amount,
    string Currency,
    decimal? AssetAmount,
    string? AssetCode,
    string? Network,
    string Status,
    string CheckoutLink,
    string? AlternativeCheckoutLink,
    string? DestinationAddress,
    string? DestinationMemo,
    string? ProviderTransactionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CreditedAtUtc);

