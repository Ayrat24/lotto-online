using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MiniApp.Admin;
using MiniApp.Features.Auth;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

public sealed class LoginModel : LocalizedAdminPageModel
{
    private readonly AdminOptions _admin;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public LoginModel(IOptions<AdminOptions> admin, IConfiguration config, IWebHostEnvironment env, ILocalizationService localization)
        : base(localization)
    {
        _admin = admin.Value;
        _config = config;
        _env = env;
    }

    [BindProperty]
    public string? Username { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        await LoadUiTextAsync(HttpContext.RequestAborted);

        if (LocalDebugMode.IsEnabled(_config, _env) && LocalDebugMode.IsLocalRequest(HttpContext))
        {
            var debugAdminUsername = LocalDebugMode.GetAdminUsername(_config);
            await AdminAuth.SignInAdminAsync(HttpContext, debugAdminUsername);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToPage("/Admin/Index");
        }

        Username ??= _admin.Username;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        await LoadUiTextAsync(HttpContext.RequestAborted);

        if (!AdminAuth.ValidateCredentials(_admin, Username, Password))
        {
            ErrorMessage = await GetTextAsync("admin.login.error.invalidCredentials", "Invalid username/password.", HttpContext.RequestAborted);
            return Page();
        }

        await AdminAuth.SignInAdminAsync(HttpContext, Username!);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Admin/Index");
    }
}

