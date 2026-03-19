using MiniApp.Data;

namespace MiniApp.Features.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/users/touch", async (TouchUserRequest req, IUserService users, CancellationToken ct) =>
        {
            if (req.TelegramUserId <= 0)
                return Results.BadRequest("TelegramUserId must be > 0");

            var u = await users.TouchUserAsync(req.TelegramUserId, ct);
            return Results.Ok(new { u.Id, u.TelegramUserId, u.CreatedAtUtc, u.LastSeenAtUtc });
        });

        return endpoints;
    }

    public sealed record TouchUserRequest(long TelegramUserId);
}

