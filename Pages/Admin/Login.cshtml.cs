using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MiniApp.Admin;

namespace MiniApp.Pages.Admin;

public sealed class LoginModel : PageModel
{
    private readonly AdminOptions _admin;

    public LoginModel(IOptions<AdminOptions> admin)
    {
        _admin = admin.Value;
    }

    [BindProperty]
    public string? Username { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        Username ??= _admin.Username;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!AdminAuth.ValidateCredentials(_admin, Username, Password))
        {
            ErrorMessage = "Invalid username/password.";
            return Page();
        }

        await AdminAuth.SignInAdminAsync(HttpContext, Username!);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Admin/Index");
    }
}

