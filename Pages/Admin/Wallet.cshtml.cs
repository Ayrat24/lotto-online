using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Localization;

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
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ReviewedAtUtc,
        string? ReviewedByAdmin,
        string? ReviewNote);

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IWalletService _wallet;

    public WalletModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env, IWalletService wallet, ILocalizationService localization)
        : base(localization)
    {
        _db = db;
        _config = config;
        _env = env;
        _wallet = wallet;
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

        PendingWithdrawalRequests = await _db.WithdrawalRequests
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
                x.CreatedAtUtc,
                x.ReviewedAtUtc,
                x.ReviewedByAdmin,
                x.PayoutLastError ?? x.ReviewNote))
            .ToArrayAsync(ct);

        RecentWithdrawalRequests = await _db.WithdrawalRequests
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
                x.CreatedAtUtc,
                x.ReviewedAtUtc,
                x.ReviewedByAdmin,
                x.PayoutLastError ?? x.ReviewNote))
            .ToArrayAsync(ct);
    }
}


