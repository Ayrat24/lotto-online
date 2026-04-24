namespace MiniApp.Data;

public enum CryptoDepositStatus
{
    AwaitingPayment = 0,
    Paid = 1,
    Confirmed = 2,
    Credited = 3,
    Expired = 4,
    Invalid = 5
}

public sealed class CryptoDepositIntent
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public MiniAppUser User { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public string PaymentMethod { get; set; } = "btcpay_crypto";

    public string? AssetCode { get; set; }

    public decimal? AssetAmount { get; set; }

    public string? Network { get; set; }

    public string Provider { get; set; } = "BTCPay";

    public string ProviderInvoiceId { get; set; } = null!;

    public string CheckoutLink { get; set; } = null!;

    public string? AlternativeCheckoutLink { get; set; }

    public string? DestinationAddress { get; set; }

    public string? DestinationMemo { get; set; }

    public string? ProviderTransactionId { get; set; }

    public CryptoDepositStatus Status { get; set; } = CryptoDepositStatus.AwaitingPayment;

    public string? LastProviderEventType { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public DateTimeOffset? PaidAtUtc { get; set; }

    public DateTimeOffset? ConfirmedAtUtc { get; set; }

    public DateTimeOffset? CreditedAtUtc { get; set; }
}

