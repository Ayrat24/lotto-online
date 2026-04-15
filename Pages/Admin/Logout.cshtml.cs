using Microsoft.AspNetCore.Mvc;
using MiniApp.Admin;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

public sealed class LogoutModel : LocalizedAdminPageModel
{
    public LogoutModel(ILocalizationService localization) : base(localization)
    {
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUiTextAsync(HttpContext.RequestAborted);
        await AdminAuth.SignOutAdminAsync(HttpContext);
        return RedirectToPage("/Admin/Login");
    }
}

