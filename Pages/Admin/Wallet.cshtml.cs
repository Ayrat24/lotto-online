using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Localization;
using MiniApp.Features.Payments;
using TonSdk.Core;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class WalletModel : LocalizedAdminPageModel
{
    public sealed record WalletTransactionRow(
        long Id,
        DateTimeOffset CreatedAtUtc,
        string Type,
        long UserId,
        long TelegramUserId,
        decimal UserDelta,
        decimal UserBalanceAfter,
        decimal ServerDelta,
        decimal ServerBalanceAfter,
        string? Reference);

    public sealed record WithdrawalRequestRow(
        long Id,
        long UserId,
        long TelegramUserId,
        decimal Amount,
        string AssetCode,
        decimal? AssetAmount,
        string Number,
        string? ExternalPayoutId,
        string Status,
        string ReviewStatus,
        string? RawPayoutState,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ReviewedAtUtc,
        string? ReviewedByAdmin,
        string? ReviewNote);

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IWalletService _wallet;
    private readonly TelegramTonWithdrawalProcessor _tonWithdrawalProcessor;
    private readonly ILogger<WalletModel> _logger;

    public WalletModel(
        AppDbContext db,
        IConfiguration config,
        IWebHostEnvironment env,
        IWalletService wallet,
        TelegramTonWithdrawalProcessor tonWithdrawalProcessor,
        ILogger<WalletModel> logger,
        ILocalizationService localization)
        : base(localization)
    {
        _db = db;
        _config = config;
        _env = env;
        _wallet = wallet;
        _tonWithdrawalProcessor = tonWithdrawalProcessor;
        _logger = logger;
    }

    public decimal ServerBalance { get; private set; }

    public IReadOnlyList<WalletTransactionRow> Transactions { get; private set; } = Array.Empty<WalletTransactionRow>();
    public IReadOnlyList<WithdrawalRequestRow> PendingWithdrawalRequests { get; private set; } = Array.Empty<WithdrawalRequestRow>();
    public IReadOnlyList<WithdrawalRequestRow> RecentWithdrawalRequests { get; private set; } = Array.Empty<WithdrawalRequestRow>();

    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            StatusMessage = FlashMessage;
            StatusIsError = FlashIsError ?? false;
        }

        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostConfirmWithdrawalAsync(long id, CancellationToken ct)
    {
        var result = await _wallet.ConfirmWithdrawalAsync(id, User.Identity?.Name ?? "admin", null, ct);
        if (result.Success)
        {
            try
            {
                await _tonWithdrawalProcessor.ProcessNextAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process the TON withdrawal queue immediately after confirming request {WithdrawalRequestId}.", id);
            }
        }

        var successTemplate = await GetTextAsync("admin.wallet.flash.confirmed", "Confirmed withdrawal request #{0}.", ct);
        var failedText = await GetTextAsync("admin.wallet.flash.confirmFailed", "Failed to confirm withdrawal request.", ct);
        FlashMessage = result.Success ? string.Format(successTemplate, id) : result.Error ?? failedText;
        FlashIsError = !result.Success;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDenyWithdrawalAsync(long id, string? note, CancellationToken ct)
    {
        var result = await _wallet.DenyWithdrawalAsync(id, User.Identity?.Name ?? "admin", note, ct);
        var successTemplate = await GetTextAsync("admin.wallet.flash.denied", "Denied withdrawal request #{0}. Funds were returned to the user.", ct);
        var failedText = await GetTextAsync("admin.wallet.flash.denyFailed", "Failed to deny withdrawal request.", ct);
        FlashMessage = result.Success ? string.Format(successTemplate, id) : result.Error ?? failedText;
        FlashIsError = !result.Success;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRefreshTonWithdrawalAsync(long id, CancellationToken ct)
    {
        var result = await _tonWithdrawalProcessor.ProcessRequestAsync(id, ct);

        FlashMessage = result.Type switch
        {
            TelegramTonWithdrawalManualProcessResultType.Processed
                => string.Format(await GetTextAsync("admin.wallet.flash.refreshTonProcessed", "Processed TON withdrawal request #{0}.", ct), id),
            TelegramTonWithdrawalManualProcessResultType.NoChange
                => string.Format(await GetTextAsync("admin.wallet.flash.refreshTonNoChange", "Checked TON withdrawal request #{0}, but no new status was found yet.", ct), id),
            TelegramTonWithdrawalManualProcessResultType.NotFound
                => string.Format(await GetTextAsync("admin.wallet.flash.refreshTonNotFound", "TON withdrawal request #{0} was not found.", ct), id),
            TelegramTonWithdrawalManualProcessResultType.Disabled
                => await GetTextAsync("admin.wallet.flash.refreshTonDisabled", "Server-executed TON withdrawals are disabled right now.", ct),
            _ => string.Format(await GetTextAsync("admin.wallet.flash.refreshTonNotEligible", "TON withdrawal request #{0} is not in a retryable state.", ct), id)
        };

        FlashIsError = result.Type is TelegramTonWithdrawalManualProcessResultType.NotFound
            or TelegramTonWithdrawalManualProcessResultType.NotEligible
            or TelegramTonWithdrawalManualProcessResultType.Disabled;
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        if (LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, ct);

        var serverWallet = await _wallet.EnsureServerWalletAsync(ct);
        ServerBalance = serverWallet.Balance;

        Transactions = await _db.WalletTransactions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(300)
            .Select(x => new WalletTransactionRow(
                x.Id,
                x.CreatedAtUtc,
                x.Type.ToString(),
                x.UserId,
                x.User.TelegramUserId,
                x.UserDelta,
                x.UserBalanceAfter,
                x.ServerDelta,
                x.ServerBalanceAfter,
                x.Reference))
            .ToArrayAsync(ct);

        PendingWithdrawalRequests = (await _db.WithdrawalRequests
            .AsNoTracking()
            .Where(x => x.Status == WithdrawalRequestStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(200)
            .Select(x => new WithdrawalRequestRow(
                x.Id,
                x.UserId,
                x.User.TelegramUserId,
                x.Amount,
                WithdrawalAssetCodes.Normalize(x.AssetCode, defaultToBitcoin: true) ?? WithdrawalAssetCodes.Bitcoin,
                x.AssetAmount,
                x.Number,
                x.ExternalPayoutId,
                x.Status.ToString(),
                x.Status.ToString(),
                x.ExternalPayoutState,
                x.CreatedAtUtc,
                x.ReviewedAtUtc,
                x.ReviewedByAdmin,
                x.PayoutLastError ?? x.ReviewNote))
            .ToArrayAsync(ct))
            .Select(x => x with { Number = NormalizeWithdrawalDisplayAddress(x.AssetCode, x.Number) })
            .ToArray();

        RecentWithdrawalRequests = (await _db.WithdrawalRequests
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(300)
            .Select(x => new WithdrawalRequestRow(
                x.Id,
                x.UserId,
                x.User.TelegramUserId,
                x.Amount,
                WithdrawalAssetCodes.Normalize(x.AssetCode, defaultToBitcoin: true) ?? WithdrawalAssetCodes.Bitcoin,
                x.AssetAmount,
                x.Number,
                x.ExternalPayoutId,
                string.IsNullOrWhiteSpace(x.ExternalPayoutState) ? x.Status.ToString() : x.ExternalPayoutState!,
                x.Status.ToString(),
                x.ExternalPayoutState,
                x.CreatedAtUtc,
                x.ReviewedAtUtc,
                x.ReviewedByAdmin,
                x.PayoutLastError ?? x.ReviewNote))
            .ToArrayAsync(ct))
            .Select(x => x with { Number = NormalizeWithdrawalDisplayAddress(x.AssetCode, x.Number) })
            .ToArray();
    }

    private string NormalizeWithdrawalDisplayAddress(string assetCode, string address)
    {
        if (!string.Equals(assetCode, WithdrawalAssetCodes.Ton, StringComparison.OrdinalIgnoreCase))
            return address;

        var trimmed = (address ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return trimmed;

        try
        {
            var tonAddress = new Address(trimmed);
            var options = new AddressStringifyOptions(true, true, false, 0)
            {
                UrlSafe = true,
                Bounceable = false,
                TestOnly = TelegramTonNetworkNames.ApiBaseUrlLooksLikeTestnet(_config["Payments:TelegramTon:ApiBaseUrl"]),
                Workchain = null
            };

            return tonAddress.ToString(AddressType.Base64, options);
        }
        catch
        {
            return trimmed;
        }
    }

    public bool CanRefreshTonWithdrawal(WithdrawalRequestRow request)
    {
        if (!string.Equals(request.AssetCode, WithdrawalAssetCodes.Ton, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(request.ReviewStatus, WithdrawalRequestStatus.Confirmed.ToString(), StringComparison.OrdinalIgnoreCase))
            return false;

        var payoutState = (request.RawPayoutState ?? string.Empty).Trim().ToLowerInvariant();
        return payoutState is TonWithdrawalPayoutStates.Queued
            or TonWithdrawalPayoutStates.RetryPending
            or TonWithdrawalPayoutStates.Sending
            or TonWithdrawalPayoutStates.Submitted;
    }
}


