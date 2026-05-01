namespace MiniApp.Features.Payments;

public sealed class TelegramTonWithdrawalWorkerState
{
    private readonly object _sync = new();

    private bool _started;
    private bool _disabledByConfiguration;
    private string? _disabledReason;
    private DateTimeOffset? _startedAtUtc;
    private DateTimeOffset? _lastCycleStartedAtUtc;
    private DateTimeOffset? _lastCycleCompletedAtUtc;
    private DateTimeOffset? _lastProgressAtUtc;
    private long _cycleCount;
    private int? _lastChangedCount;
    private string? _lastError;
    private double? _intervalSeconds;

    public void MarkDisabled(string reason)
    {
        lock (_sync)
        {
            _started = false;
            _disabledByConfiguration = true;
            _disabledReason = Truncate(reason);
            _intervalSeconds = null;
        }
    }

    public void MarkStarted(TimeSpan interval)
    {
        lock (_sync)
        {
            _started = true;
            _disabledByConfiguration = false;
            _disabledReason = null;
            _startedAtUtc ??= DateTimeOffset.UtcNow;
            _intervalSeconds = interval.TotalSeconds;
        }
    }

    public void MarkCycleStarted()
    {
        lock (_sync)
        {
            _lastCycleStartedAtUtc = DateTimeOffset.UtcNow;
            _cycleCount += 1;
        }
    }

    public void MarkCycleCompleted(int changedCount)
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            _lastCycleCompletedAtUtc = now;
            _lastChangedCount = changedCount;
            _lastError = null;
            if (changedCount > 0)
                _lastProgressAtUtc = now;
        }
    }

    public void MarkCycleFailed(Exception ex)
    {
        lock (_sync)
        {
            _lastCycleCompletedAtUtc = DateTimeOffset.UtcNow;
            _lastChangedCount = null;
            _lastError = Truncate(ex.Message);
        }
    }

    public TonWithdrawalWorkerRuntimeView Snapshot()
    {
        lock (_sync)
        {
            return new TonWithdrawalWorkerRuntimeView(
                _started,
                _disabledByConfiguration,
                _disabledReason,
                _startedAtUtc,
                _lastCycleStartedAtUtc,
                _lastCycleCompletedAtUtc,
                _lastProgressAtUtc,
                _cycleCount,
                _lastChangedCount,
                _lastError,
                _intervalSeconds);
        }
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Length <= 512 ? value : value[..512];
    }
}

