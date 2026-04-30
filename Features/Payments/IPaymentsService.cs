namespace MiniApp.Features.Payments;

public interface IPaymentsService
{
    PaymentSystemsView GetPaymentSystems();

    Task<CreateCryptoDepositResult> CreateCryptoDepositAsync(long userId, decimal amount, string? currency, string? paymentMethod, CancellationToken ct);

    Task<CryptoDepositStatusResult> GetCryptoDepositStatusAsync(long userId, long depositId, CancellationToken ct);

    Task<int> ReconcilePendingTelegramTonDepositsAsync(CancellationToken ct);

    Task<ProcessWebhookResult> ProcessBtcPayWebhookAsync(string payloadJson, string? deliveryId, string? signature, CancellationToken ct);
}

