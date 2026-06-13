using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MiniApp.Admin;
using MiniApp.Features.Auth;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

public sealed class LoginModel : LocalizedAdminPageModel
{
    private const int MaxFailedAttemptsPerWindow = 8;
    private static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan FailedAttemptDelay = TimeSpan.FromMilliseconds(750);

    private readonly AdminOptions _admin;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache _cache;

    public LoginModel(IOptions<AdminOptions> admin, IConfiguration config, IWebHostEnvironment env, ILocalizationService localization, IMemoryCache cache)
        : base(localization)
    {
        _admin = admin.Value;
        _config = config;
        _env = env;
        _cache = cache;
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

        var throttleKey = GetThrottleKey();
        if (GetFailedAttempts(throttleKey) >= MaxFailedAttemptsPerWindow)
        {
            ErrorMessage = await GetTextAsync(
                "admin.login.error.tooManyAttempts",
                "Too many failed attempts. Please wait a few minutes and try again.",
                HttpContext.RequestAborted);
            return Page();
        }

        if (!AdminAuth.ValidateCredentials(_admin, Username, Password))
        {
            RegisterFailedAttempt(throttleKey);
            // Slow down automated brute-force attempts.
            await Task.Delay(FailedAttemptDelay, HttpContext.RequestAborted);
            ErrorMessage = await GetTextAsync("admin.login.error.invalidCredentials", "Invalid username/password.", HttpContext.RequestAborted);
            return Page();
        }

        _cache.Remove(throttleKey);
        await AdminAuth.SignInAdminAsync(HttpContext, Username!);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToPage("/Admin/Index");
    }

    private string GetThrottleKey()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"admin-login-failures:{ip}";
    }

    private int GetFailedAttempts(string key)
        => _cache.TryGetValue(key, out int count) ? count : 0;

    private void RegisterFailedAttempt(string key)
    {
        var count = GetFailedAttempts(key) + 1;
        _cache.Set(key, count, FailedAttemptWindow);
    }
}

