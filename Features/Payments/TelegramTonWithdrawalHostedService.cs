using Microsoft.Extensions.Options;

namespace MiniApp.Features.Payments;

public sealed class TelegramTonWithdrawalHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IOptions<PaymentsOptions> _options;
    private readonly ILogger<TelegramTonWithdrawalHostedService> _logger;

    public TelegramTonWithdrawalHostedService(
        IServiceProvider services,
        IOptions<PaymentsOptions> options,
        ILogger<TelegramTonWithdrawalHostedService> logger)
    {
        _services = services;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var telegramTon = _options.Value.TelegramTon;
        if (!_options.Value.Enabled || !telegramTon.Enabled || !telegramTon.ServerWithdrawalsEnabled)
        {
            _logger.LogInformation("Telegram TON withdrawal worker is disabled.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Clamp(telegramTon.WithdrawalWorkerIntervalSeconds, 1, 300));
        _logger.LogInformation("Starting Telegram TON withdrawal worker with interval {DelaySeconds}s.", delay.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<TelegramTonWithdrawalProcessor>();
                var changed = await processor.ProcessNextAsync(stoppingToken);
                if (changed > 0)
                {
                    _logger.LogInformation("Telegram TON withdrawal worker processed {ChangedCount} item(s).", changed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram TON withdrawal worker cycle failed.");
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

