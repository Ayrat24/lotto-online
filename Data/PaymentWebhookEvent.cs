namespace MiniApp.Data;

public enum PaymentWebhookEventStatus
{
    Received = 0,
    Processed = 1,
    Ignored = 2,
    Failed = 3
}

public sealed class PaymentWebhookEvent
{
    public long Id { get; set; }

    public string Provider { get; set; } = "BTCPay";

    public string? DeliveryId { get; set; }

    public string? EventType { get; set; }

    public string? ProviderObjectId { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public PaymentWebhookEventStatus Status { get; set; } = PaymentWebhookEventStatus.Received;

    public string? Error { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ProcessedAtUtc { get; set; }
}

