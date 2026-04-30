using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniApp.Admin;
using MiniApp.Features.Localization;
using MiniApp.Features.Payments;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class TonDepositsModel : LocalizedAdminPageModel
{
    private readonly IPaymentsService _payments;

    public TonDepositsModel(IPaymentsService payments, ILocalizationService localization)
        : base(localization)
    {
        _payments = payments;
    }

    [BindProperty(SupportsGet = true)]
    public int Limit { get; set; } = 25;

    public IReadOnlyList<TelegramTonAdminDepositDiagnosticView> Deposits { get; private set; } = Array.Empty<TelegramTonAdminDepositDiagnosticView>();

    public string? StatusMessage { get; private set; }

    public bool StatusIsError { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostReconcileAsync(long id, int? limit, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        Limit = Math.Clamp(limit ?? Limit, 1, 100);

        var result = await _payments.ReconcileTelegramTonAdminDepositAsync(id, ct);
        if (!result.Success)
        {
            StatusMessage = result.Error ?? await GetTextAsync("admin.tonDeposits.flash.reconcileFailed", "Failed to reconcile TON deposit.", ct);
            StatusIsError = true;
            await LoadAsync(ct);
            return Page();
        }

        var templateKey = result.Changed
            ? "admin.tonDeposits.flash.reconciledChanged"
            : "admin.tonDeposits.flash.reconciledNoChange";
        var fallback = result.Changed
            ? "Reconciled TON deposit #{0}."
            : "TON deposit #{0} was checked but nothing changed.";
        var template = await GetTextAsync(templateKey, fallback, ct);
        StatusMessage = string.Format(template, id);
        StatusIsError = false;
        await LoadAsync(ct);
        if (result.Deposit is not null)
        {
            Deposits = Deposits
                .Select(x => x.Id == result.Deposit.Id ? result.Deposit : x)
                .ToArray();
        }

        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Limit = Math.Clamp(Limit, 1, 100);
        var result = await _payments.GetTelegramTonAdminDepositDiagnosticsAsync(Limit, ct);
        if (!result.Success)
        {
            StatusMessage = result.Error ?? T("admin.tonDeposits.flash.loadFailed", "Failed to load TON deposit diagnostics.");
            StatusIsError = true;
            Deposits = Array.Empty<TelegramTonAdminDepositDiagnosticView>();
            return;
        }

        Deposits = result.Deposits;
    }
}


