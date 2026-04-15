using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniApp.Admin;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin
{
    [Authorize(Policy = AdminAuth.PolicyName)]
    public sealed class DangerZoneModel : LocalizedAdminPageModel
    {
        private readonly DatabaseResetService _databaseResetService;
        private readonly ILogger<DangerZoneModel> _logger;

        public DangerZoneModel(DatabaseResetService databaseResetService, ILogger<DangerZoneModel> logger, ILocalizationService localization)
            : base(localization)
        {
            _databaseResetService = databaseResetService;
            _logger = logger;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync(CancellationToken ct) => await LoadUiTextAsync(ct);

        public async Task<IActionResult> OnPostDropAllTablesAsync(CancellationToken ct)
        {
            try
            {
                await _databaseResetService.ResetPublicSchemaAsync(ct);
                StatusMessage = await GetTextAsync("admin.danger.flash.success", "Database reset completed. The schema was recreated and migrations were applied.", ct);
                return RedirectToPage();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin database reset failed.");
                await LoadUiTextAsync(ct);
                ErrorMessage = await GetTextAsync("admin.danger.flash.failed", "Database reset failed. Check application logs for details.", ct);
                return Page();
            }

        }
    }
}
