namespace MiniApp.Features.Payments;

public sealed record CreateCryptoDepositRequest(string InitData, decimal Amount, string? Currency);

public sealed record GetCryptoDepositStatusRequest(string InitData, long DepositId);

public sealed record CreateCryptoDepositResult(bool Success, string? Error, CryptoDepositView? Deposit = null);

public sealed record CryptoDepositStatusResult(bool Success, string? Error, CryptoDepositView? Deposit = null);

public sealed record ProcessWebhookResult(bool Success, string? Error, bool Duplicate = false);

public sealed record CryptoDepositView(
    long Id,
    string Provider,
    string ProviderInvoiceId,
    decimal Amount,
    string Currency,
    string Status,
    string CheckoutLink,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CreditedAtUtc);

