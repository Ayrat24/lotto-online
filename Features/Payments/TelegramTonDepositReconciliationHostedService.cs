using Microsoft.Extensions.Options;

namespace MiniApp.Features.Payments;

public sealed class TelegramTonDepositReconciliationHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IOptions<PaymentsOptions> _options;
    private readonly ILogger<TelegramTonDepositReconciliationHostedService> _logger;

    public TelegramTonDepositReconciliationHostedService(
        IServiceProvider services,
        IOptions<PaymentsOptions> options,
        ILogger<TelegramTonDepositReconciliationHostedService> logger)
    {
        _services = services;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled || !options.TelegramTon.Enabled)
        {
            _logger.LogInformation("Telegram TON deposit reconciliation is disabled.");
            return;
        }

        var intervalSeconds = Math.Clamp(options.TelegramTon.ReconciliationIntervalSeconds, 1, 300);
        var delay = TimeSpan.FromSeconds(intervalSeconds);

        _logger.LogInformation(
            "Starting Telegram TON deposit reconciliation worker with interval {IntervalSeconds}s.",
            intervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var payments = scope.ServiceProvider.GetRequiredService<IPaymentsService>();
                var changed = await payments.ReconcilePendingTelegramTonDepositsAsync(stoppingToken);
                if (changed > 0)
                {
                    _logger.LogInformation(
                        "Telegram TON deposit reconciliation updated {ChangedCount} pending deposit(s).",
                        changed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram TON deposit reconciliation cycle failed.");
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

