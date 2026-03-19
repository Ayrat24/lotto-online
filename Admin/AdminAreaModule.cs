using MiniApp.Admin;

namespace MiniApp.Admin;

public static class AdminAreaModule
{
    public static IServiceCollection AddAdminArea(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAdminAuth(configuration);
        return services;
    }
}

