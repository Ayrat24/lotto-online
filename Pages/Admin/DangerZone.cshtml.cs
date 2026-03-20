using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MiniApp.Admin;
using MiniApp.Data;

namespace MiniApp.Pages.Admin
{
    [Authorize(Policy = AdminAuth.PolicyName)]
    public sealed class DangerZoneModel : PageModel
    {
        private readonly AppDbContext db;

        public DangerZoneModel(AppDbContext db) => this.db = db;

        public string? StatusMessage { get; private set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostDropAllTablesAsync(CancellationToken ct)
        {
            // Drop everything in public schema. EF migrations will recreate schema on next start if AutoMigrate is enabled.
            await db.Database.ExecuteSqlRawAsync(@"
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO public;
", ct);

            StatusMessage = "Dropped all tables (public schema reset).";
            return Page();
        }
    }
}
