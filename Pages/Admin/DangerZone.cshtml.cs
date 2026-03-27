using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniApp.Admin;

namespace MiniApp.Pages.Admin
{
    [Authorize(Policy = AdminAuth.PolicyName)]
    public sealed class DangerZoneModel : PageModel
    {
        private readonly DatabaseResetService _databaseResetService;
        private readonly ILogger<DangerZoneModel> _logger;

        public DangerZoneModel(DatabaseResetService databaseResetService, ILogger<DangerZoneModel> logger)
        {
            _databaseResetService = databaseResetService;
            _logger = logger;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostDropAllTablesAsync(CancellationToken ct)
        {
            try
            {
                await _databaseResetService.ResetPublicSchemaAsync(ct);
                StatusMessage = "Database reset completed. The schema was recreated and migrations were applied.";
                return RedirectToPage();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin database reset failed.");
                ErrorMessage = "Database reset failed. Check application logs for details.";
                return Page();
            }

        }
    }
}
