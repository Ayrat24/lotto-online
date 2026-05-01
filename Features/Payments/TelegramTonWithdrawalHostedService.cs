using Microsoft.Extensions.Options;

namespace MiniApp.Features.Payments;

public sealed class TelegramTonWithdrawalHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IOptions<PaymentsOptions> _options;
    private readonly TelegramTonWithdrawalWorkerState _state;
    private readonly ILogger<TelegramTonWithdrawalHostedService> _logger;

    public TelegramTonWithdrawalHostedService(
        IServiceProvider services,
        IOptions<PaymentsOptions> options,
        TelegramTonWithdrawalWorkerState state,
        ILogger<TelegramTonWithdrawalHostedService> logger)
    {
        _services = services;
        _options = options;
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var telegramTon = _options.Value.TelegramTon;
        if (!_options.Value.Enabled || !telegramTon.Enabled || !telegramTon.ServerWithdrawalsEnabled)
        {
            var reason = !_options.Value.Enabled
                ? "Payments are disabled."
                : !telegramTon.Enabled
                    ? "Telegram TON payments are disabled."
                    : "Server-executed TON withdrawals are disabled.";
            _state.MarkDisabled(reason);
            _logger.LogInformation("Telegram TON withdrawal worker is disabled.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Clamp(telegramTon.WithdrawalWorkerIntervalSeconds, 1, 300));
        _state.MarkStarted(delay);
        _logger.LogInformation("Starting Telegram TON withdrawal worker with interval {DelaySeconds}s.", delay.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _state.MarkCycleStarted();
                await using var scope = _services.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<TelegramTonWithdrawalProcessor>();
                var changed = await processor.ProcessNextAsync(stoppingToken);
                _state.MarkCycleCompleted(changed);
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
                _state.MarkCycleFailed(ex);
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

