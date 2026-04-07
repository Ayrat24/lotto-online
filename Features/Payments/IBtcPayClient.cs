namespace MiniApp.Features.Payments;

public interface IBtcPayClient
{
    Task<BtcPayCreateInvoiceResult> CreateInvoiceAsync(decimal amount, string currency, string orderId, CancellationToken ct);

    Task<BtcPayGetInvoiceResult> GetInvoiceAsync(string invoiceId, CancellationToken ct);
}

public enum BtcPayErrorCode
{
    None = 0,
    Configuration,
    InvalidRequest,
    Unauthorized,
    NotFound,
    RateLimited,
    Transient,
    Upstream,
    Parse,
    Canceled
}

public sealed record BtcPayCreateInvoiceResult(
    bool Success,
    string? Error,
    BtcPayErrorCode ErrorCode = BtcPayErrorCode.None,
    string? InvoiceId = null,
    string? CheckoutLink = null,
    DateTimeOffset? ExpirationTimeUtc = null);

public sealed record BtcPayGetInvoiceResult(
    bool Success,
    string? Error,
    BtcPayErrorCode ErrorCode = BtcPayErrorCode.None,
    string? InvoiceId = null,
    string? Status = null,
    decimal? Amount = null,
    string? Currency = null,
    string? CheckoutLink = null,
    DateTimeOffset? ExpirationTimeUtc = null);

