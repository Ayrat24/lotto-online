using MiniApp.Data;
using MiniApp.Features.Auth;
using MiniApp.TelegramLogin;

namespace MiniApp.Features.Referrals;

public static class ReferralsEndpoints
{
    public static IEndpointRouteBuilder MapReferralsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/referrals/check", async (
            ReferralCheckRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            ILoggerFactory loggerFactory,
            AppDbContext db,
            IUserService users,
            IReferralService referrals,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("ReferralCheck");
            var initData = req.InitData ?? string.Empty;
            var inviteCode = req.InviteCode ?? string.Empty;
            logger.LogInformation("Referral check request started. TraceId={TraceId}, InitDataLength={InitDataLength}, CodeLength={CodeLength}", http.TraceIdentifier, initData.Length, inviteCode.Length);

            var authResult = await TryResolveTelegramUserIdAsync(initData, http, config, env, db, ct);
            if (authResult.ErrorResult is not null)
            {
                logger.LogWarning("Referral check auth failed. TraceId={TraceId}", http.TraceIdentifier);
                return authResult.ErrorResult;
            }

            var telegramUserId = authResult.TelegramUserId!.Value;
            var user = await users.TouchUserAsync(telegramUserId, ct);
            var check = await referrals.CheckCodeAsync(user.Id, inviteCode, ct);
            if (!check.Success)
            {
                logger.LogWarning("Referral check rejected. UserId={UserId}, TelegramUserId={TelegramUserId}, Error={Error}", user.Id, telegramUserId, check.Error);
                return Results.BadRequest(new { ok = false, error = check.Error ?? "Invite code is invalid." });
            }

            var settings = await referrals.GetSettingsAsync(ct);
            logger.LogInformation("Referral check succeeded. UserId={UserId}, TelegramUserId={TelegramUserId}, InviterUserId={InviterUserId}", user.Id, telegramUserId, check.InviterUserId);

            return Results.Ok(new
            {
                ok = true,
                rewardsEnabled = settings.Enabled,
                bonusAmount = settings.InviteeBonusAmount,
                minDepositAmount = settings.MinQualifyingDepositAmount
            });
        });

        endpoints.MapPost("/api/referrals/me", async (
            ReferralProfileRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            AppDbContext db,
            IUserService users,
            IReferralService referrals,
            CancellationToken ct) =>
        {
            var authResult = await TryResolveTelegramUserIdAsync(req.InitData, http, config, env, db, ct);
            if (authResult.ErrorResult is not null)
                return authResult.ErrorResult;

            var telegramUserId = authResult.TelegramUserId!.Value;
            var user = await users.TouchUserAsync(telegramUserId, ct);
            var profile = await referrals.GetProfileAsync(user.Id, ct);

            var inviteLink = BuildInviteLink(http, profile.InviteCode);
            var dto = new ReferralProfileDto(
                profile.InviteCode,
                inviteLink,
                profile.ReferredByUserId != MiniAppUser.UnboundReferralUserId,
                profile.TotalInviterRewards,
                profile.TotalInviteeRewards,
                profile.SuccessfulInvites,
                profile.MonthInviterRewards,
                profile.MonthInviterCap,
                profile.MonthInviterCap > 0m && profile.MonthInviterRewards >= profile.MonthInviterCap);

            return Results.Ok(new { ok = true, profile = dto });
        });

        endpoints.MapPost("/api/referrals/bind", async (
            ReferralBindRequest req,
            HttpContext http,
            IConfiguration config,
            IWebHostEnvironment env,
            ILoggerFactory loggerFactory,
            AppDbContext db,
            IUserService users,
            IReferralService referrals,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("ReferralBind");
            var initData = req.InitData ?? string.Empty;
            logger.LogInformation("Referral bind request started. TraceId={TraceId}, InitDataLength={InitDataLength}", http.TraceIdentifier, initData.Length);

            var authResult = await TryResolveTelegramUserIdAsync(initData, http, config, env, db, ct);
            if (authResult.ErrorResult is not null)
            {
                logger.LogWarning("Referral bind auth failed. TraceId={TraceId}", http.TraceIdentifier);
                return authResult.ErrorResult;
            }

            var telegramUserId = authResult.TelegramUserId!.Value;
            var user = await users.TouchUserAsync(telegramUserId, ct);
            var bind = await referrals.BindByCodeAsync(user.Id, req.InviteCode, ct);
            if (!bind.Success)
            {
                logger.LogWarning("Referral bind rejected. UserId={UserId}, TelegramUserId={TelegramUserId}, Error={Error}", user.Id, telegramUserId, bind.Error);
                return Results.BadRequest(new { ok = false, error = bind.Error ?? "Failed to bind invite code." });
            }

            logger.LogInformation("Referral bind succeeded. UserId={UserId}, TelegramUserId={TelegramUserId}", user.Id, telegramUserId);

            var settings = await referrals.GetSettingsAsync(ct);
            return Results.Ok(new
            {
                ok = true,
                rewardsEnabled = settings.Enabled,
                bonusAmount = settings.InviteeBonusAmount,
                minDepositAmount = settings.MinQualifyingDepositAmount
            });
        });

        return endpoints;
    }

    private static string BuildInviteLink(HttpContext http, string inviteCode)
    {
        var scheme = string.IsNullOrWhiteSpace(http.Request.Scheme) ? "https" : http.Request.Scheme;
        var host = http.Request.Host.HasValue ? http.Request.Host.Value : "localhost";
        var safeCode = Uri.EscapeDataString(inviteCode);
        return $"{scheme}://{host}/app?ref={safeCode}";
    }

    private static async Task<(long? TelegramUserId, IResult? ErrorResult)> TryResolveTelegramUserIdAsync(
        string initData,
        HttpContext http,
        IConfiguration config,
        IWebHostEnvironment env,
        AppDbContext db,
        CancellationToken ct)
    {
        if (LocalDebugMode.TryGetDebugTelegramUserId(http, config, env, out var localDebugUserId))
        {
            await LocalDebugSeed.EnsureSeededAsync(db, localDebugUserId, ct);
            return (localDebugUserId, null);
        }

        var botToken = config["BotToken"];
        if (string.IsNullOrWhiteSpace(botToken))
            return (null, Results.Problem("BotToken is not configured.", statusCode: 500));

        if (!TelegramInitDataValidator.TryValidateInitData(initData, botToken, TimeSpan.FromMinutes(10), out var tgUser, out var error))
        {
            if (env.IsDevelopment())
                return (null, Results.Json(new { ok = false, error }, statusCode: StatusCodes.Status401Unauthorized));
            return (null, Results.Unauthorized());
        }

        return (tgUser!.Id, null);
    }

}

