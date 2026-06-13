using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace MiniApp.Admin;

public static class AdminAuth
{
    public const string PolicyName = "AdminOnly";

    public static IServiceCollection AddAdminAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Username), "Admin:Username is required")
            // Password can be empty in dev, but it's strongly recommended to set it.
            .ValidateOnStart();

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Admin/Login";
                options.LogoutPath = "/Admin/Logout";
                options.AccessDeniedPath = "/Admin/Login";
                options.Cookie.Name = ".MiniApp.Admin";
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyName, policy =>
                policy.RequireAuthenticatedUser().RequireClaim(ClaimTypes.Role, "Admin"));
        });

        return services;
    }

    public static bool ValidateCredentials(AdminOptions opts, string? username, string? password)
    {
        // Constant-time comparison of both fields to avoid leaking credentials via timing.
        // Both comparisons are always evaluated (no short-circuit) for the same reason.
        var usernameOk = FixedTimeEquals(opts.Username, username);
        var passwordOk = FixedTimeEquals(opts.Password, password);
        return usernameOk && passwordOk;
    }

    private static bool FixedTimeEquals(string? expected, string? actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected ?? string.Empty);
        var actualBytes = Encoding.UTF8.GetBytes(actual ?? string.Empty);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    public static ClaimsPrincipal CreateAdminPrincipal(string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static async Task SignInAdminAsync(HttpContext http, string username)
    {
        var principal = CreateAdminPrincipal(username);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    public static Task SignOutAdminAsync(HttpContext http)
        => http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}

