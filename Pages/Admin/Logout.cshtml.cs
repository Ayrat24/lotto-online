using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniApp.Admin;

namespace MiniApp.Pages.Admin;

public sealed class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await AdminAuth.SignOutAdminAsync(HttpContext);
        return RedirectToPage("/Admin/Login");
    }
}

