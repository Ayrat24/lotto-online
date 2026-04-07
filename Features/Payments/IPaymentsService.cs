namespace MiniApp.Features.Payments;

public interface IPaymentsService
{
    Task<CreateCryptoDepositResult> CreateCryptoDepositAsync(long userId, decimal amount, string? currency, CancellationToken ct);

    Task<CryptoDepositStatusResult> GetCryptoDepositStatusAsync(long userId, long depositId, CancellationToken ct);

    Task<ProcessWebhookResult> ProcessBtcPayWebhookAsync(string payloadJson, string? deliveryId, string? signature, CancellationToken ct);
}

