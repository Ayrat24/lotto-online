using Microsoft.Extensions.Options;

namespace MiniApp.Features.Payments;

public interface ITelegramTonRateService
{
	Task<TelegramTonResolvedRate?> GetResolvedRateAsync(CancellationToken ct);
	Task RefreshAsync(CancellationToken ct);
}

public sealed class TelegramTonResolvedRate
{
	public decimal UsdPerTon { get; init; }

	public DateTimeOffset ObservedAtUtc { get; init; }

	public string Source { get; init; } = string.Empty;

	public bool IsFallback { get; init; }

	public bool IsStale { get; init; }
}

public sealed class TelegramTonRateService : ITelegramTonRateService
{
	private readonly ITelegramTonClient _client;
	private readonly PaymentsOptions _options;
	private readonly ILogger<TelegramTonRateService> _logger;
	private readonly SemaphoreSlim _refreshLock = new(1, 1);
	private TelegramTonUsdRateQuote? _cachedQuote;

	public TelegramTonRateService(
		ITelegramTonClient client,
		IOptions<PaymentsOptions> options,
		ILogger<TelegramTonRateService> logger)
	{
		_client = client;
		_options = options.Value;
		_logger = logger;
	}

	public async Task<TelegramTonResolvedRate?> GetResolvedRateAsync(CancellationToken ct)
	{
		var ton = _options.TelegramTon;
		if (!ton.Enabled)
			return null;

		if (ton.AutoRefreshEnabled)
		{
			if (ShouldRefresh(DateTimeOffset.UtcNow))
				await RefreshAsync(ct);

			var cached = _cachedQuote;
			if (cached is not null)
			{
				var age = DateTimeOffset.UtcNow - cached.ObservedAtUtc;
				if (age <= TimeSpan.FromMinutes(Math.Max(ton.MaxRateAgeMinutes, 1)))
				{
					return new TelegramTonResolvedRate
					{
						UsdPerTon = cached.UsdPerTon,
						ObservedAtUtc = cached.ObservedAtUtc,
						Source = cached.Source,
						IsFallback = false,
						IsStale = age > TimeSpan.FromMinutes(Math.Max(ton.RateRefreshIntervalMinutes, 1))
					};
				}
			}
		}

		if (ton.UsdPerTon > 0m)
		{
			return new TelegramTonResolvedRate
			{
				UsdPerTon = ton.UsdPerTon,
				ObservedAtUtc = DateTimeOffset.UtcNow,
				Source = "config-fallback",
				IsFallback = true,
				IsStale = false
			};
		}

		return null;
	}

	public async Task RefreshAsync(CancellationToken ct)
	{
		var ton = _options.TelegramTon;
		if (!ton.Enabled || !ton.AutoRefreshEnabled)
			return;

		await _refreshLock.WaitAsync(ct);
		try
		{
			if (!ShouldRefresh(DateTimeOffset.UtcNow))
				return;

			var result = await _client.GetUsdPerTonRateAsync(ct);
			if (!result.Success || result.Quote is null)
			{
				_logger.LogWarning("Failed to refresh USD/TON rate: {Error}", result.Error ?? "Unknown error.");
				return;
			}

			_cachedQuote = result.Quote;
			_logger.LogInformation("Refreshed USD/TON rate: {UsdPerTon} from {Source} at {ObservedAtUtc:u}", result.Quote.UsdPerTon, result.Quote.Source, result.Quote.ObservedAtUtc);
		}
		finally
		{
			_refreshLock.Release();
		}
	}

	private bool ShouldRefresh(DateTimeOffset now)
	{
		var cached = _cachedQuote;
		if (cached is null)
			return true;

		return now - cached.ObservedAtUtc >= TimeSpan.FromMinutes(Math.Max(_options.TelegramTon.RateRefreshIntervalMinutes, 1));
	}
}

public sealed class TelegramTonRateRefreshHostedService : BackgroundService
{
	private readonly ITelegramTonRateService _rateService;
	private readonly PaymentsOptions _options;
	private readonly ILogger<TelegramTonRateRefreshHostedService> _logger;

	public TelegramTonRateRefreshHostedService(
		ITelegramTonRateService rateService,
		IOptions<PaymentsOptions> options,
		ILogger<TelegramTonRateRefreshHostedService> logger)
	{
		_rateService = rateService;
		_options = options.Value;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!_options.Enabled || !_options.TelegramTon.Enabled || !_options.TelegramTon.AutoRefreshEnabled)
			return;

		try
		{
			await _rateService.RefreshAsync(stoppingToken);
		}
		catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
		{
			_logger.LogWarning(ex, "Initial USD/TON rate refresh failed.");
		}

		using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(_options.TelegramTon.RateRefreshIntervalMinutes, 1)));
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			try
			{
				await _rateService.RefreshAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Scheduled USD/TON rate refresh failed.");
			}
		}
	}
}


