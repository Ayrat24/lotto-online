using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.Features.Draws;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class DrawsModel : LocalizedAdminPageModel
{
    public sealed record AdminDrawRow(
        long Id,
        decimal PrizePoolMatch3,
        decimal PrizePoolMatch4,
        decimal PrizePoolMatch5,
        decimal TicketCost,
        string State,
        string? Numbers,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset PurchaseClosesAtUtc,
        long TicketCount)
    {
        public decimal TotalPrizePool => PrizePoolMatch3 + PrizePoolMatch4 + PrizePoolMatch5;
    }

    public sealed record TicketPurchaseSettingsView(
        int TicketSlotsCount,
        DateTimeOffset UpdatedAtUtc,
        string? UpdatedByAdmin);

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ITicketPurchaseSettingsService _ticketPurchaseSettings;

    public DrawsModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env, ITicketPurchaseSettingsService ticketPurchaseSettings, ILocalizationService localization)
        : base(localization)
    {
        _db = db;
        _config = config;
        _env = env;
        _ticketPurchaseSettings = ticketPurchaseSettings;
    }

    public IReadOnlyList<AdminDrawRow> Draws { get; private set; } = Array.Empty<AdminDrawRow>();
    public TicketPurchaseSettingsView PurchaseSettings { get; private set; } = new(10, DateTimeOffset.MinValue, null);
    public string? StatusMessage { get; private set; }
    public bool StatusIsError { get; private set; }

    [BindProperty]
    public int TicketSlotsCount { get; set; } = 10;

    [BindProperty]
    public string CreatePurchaseClosesAtUtc { get; set; } = string.Empty;

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public bool? FlashIsError { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);
        if (!string.IsNullOrWhiteSpace(FlashMessage))
        {
            StatusMessage = FlashMessage;
            StatusIsError = FlashIsError ?? false;
        }
        await LoadAsync(ct);
    }

    public async Task<IActionResult> OnPostCreateAsync(decimal prizePoolMatch3, decimal prizePoolMatch4, decimal prizePoolMatch5, decimal ticketCost, CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);

        if (!DrawManagement.TryParseAdminUtcInput(CreatePurchaseClosesAtUtc, out var purchaseClosesAtUtc))
        {
            StatusMessage = await GetTextAsync("admin.draws.flash.invalidPurchaseClosesAtUtc", "Enter a valid UTC purchase close date and time.", ct);
            StatusIsError = true;
            await LoadAsync(ct);
            return Page();
        }

        try
        {
            var draw = await DrawManagement.CreateDrawAsync(_db, prizePoolMatch3, prizePoolMatch4, prizePoolMatch5, ticketCost, purchaseClosesAtUtc, ct);
            var template = await GetTextAsync("admin.draws.flash.created", "Created draw #{0} in {1} state.", ct);
            StatusMessage = string.Format(template, draw.Id, DrawManagement.ToStateValue(draw.State));
            StatusIsError = false;
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusIsError = true;
        }

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostSavePurchaseSettingsAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
        await EnsureDebugSeedAsync(ct);

        if (TicketSlotsCount < 1 || TicketSlotsCount > 50)
        {
            StatusMessage = await GetTextAsync("admin.draws.purchaseSettings.flash.invalidRange", "Ticket screen slot count must be between 1 and 50.", ct);
            StatusIsError = true;
            await LoadAsync(ct);
            return Page();
        }

        await _ticketPurchaseSettings.SaveSettingsAsync(TicketSlotsCount, User.Identity?.Name ?? "admin", ct);
        StatusMessage = await GetTextAsync("admin.draws.purchaseSettings.flash.saved", "Ticket purchase screen settings were saved.", ct);
        StatusIsError = false;
        await LoadAsync(ct);
        return Page();
    }


    private async Task LoadAsync(CancellationToken ct)
    {
        var purchaseSettings = await _ticketPurchaseSettings.GetSettingsAsync(ct);
        PurchaseSettings = new TicketPurchaseSettingsView(
            purchaseSettings.TicketSlotsCount,
            purchaseSettings.UpdatedAtUtc,
            purchaseSettings.UpdatedByAdmin);
        TicketSlotsCount = purchaseSettings.TicketSlotsCount;
        if (string.IsNullOrWhiteSpace(CreatePurchaseClosesAtUtc))
            CreatePurchaseClosesAtUtc = DrawManagement.FormatAdminUtcInput(DrawManagement.GetDefaultPurchaseClosesAtUtc(DateTimeOffset.UtcNow));

        var ticketCounts = await _db.Tickets
            .AsNoTracking()
            .GroupBy(x => x.DrawId)
            .Select(g => new
            {
                DrawId = g.Key,
                TicketCount = g.LongCount()
            })
            .ToDictionaryAsync(x => x.DrawId, x => x.TicketCount, ct);

        var draws = await _db.Draws
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(200)
            .ToListAsync(ct);

        Draws = draws
            .Select(draw => new AdminDrawRow(
                draw.Id,
                draw.PrizePoolMatch3,
                draw.PrizePoolMatch4,
                draw.PrizePoolMatch5,
                draw.TicketCost,
                DrawManagement.ToStateValue(draw.State),
                draw.Numbers,
                draw.CreatedAtUtc,
                draw.PurchaseClosesAtUtc,
                ticketCounts.GetValueOrDefault(draw.Id)))
            .ToArray();
    }

    private async Task EnsureDebugSeedAsync(CancellationToken ct)
    {
        if (!LocalDebugMode.TryGetDebugTelegramUserId(HttpContext, _config, _env, out var debugTelegramUserId))
            return;

        await LocalDebugSeed.EnsureSeededAsync(_db, debugTelegramUserId, ct);
    }
}
