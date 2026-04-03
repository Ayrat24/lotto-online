using System.Net;

namespace MiniApp.Features.Auth;

public static class LocalDebugMode
{
    private const long DefaultDebugTelegramUserId = 900000001;

    public static bool IsEnabled(IConfiguration config, IWebHostEnvironment env)
        => env.IsDevelopment() && config.GetValue<bool>("LocalDebug:Enabled");

    public static bool IsLocalRequest(HttpContext http)
    {
        var remoteIp = http.Connection.RemoteIpAddress;
        if (remoteIp is null)
            return true;

        return IPAddress.IsLoopback(remoteIp);
    }

    public static bool TryGetDebugTelegramUserId(
        HttpContext _,
        IConfiguration config,
        IWebHostEnvironment env,
        out long telegramUserId)
    {
        telegramUserId = 0;

        if (!IsEnabled(config, env))
            return false;

        var fromConfig = config.GetValue<long?>("LocalDebug:TelegramUserId");
        telegramUserId = fromConfig is > 0 ? fromConfig.Value : DefaultDebugTelegramUserId;
        return true;
    }


    public static string GetAdminUsername(IConfiguration config)
        => config["LocalDebug:AdminUsername"] ?? "local-admin";
}

