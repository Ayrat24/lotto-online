using Microsoft.EntityFrameworkCore;
using MiniApp.Data;

namespace MiniApp.Features.Winners;

public static class WinnersEndpoints
{
	public static IEndpointRouteBuilder MapWinnersEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/api/winners", async (AppDbContext db, CancellationToken ct) =>
		{
			var winners = await db.Set<WinnerEntry>()
				.AsNoTracking()
				.Where(x => x.IsPublished)
				.OrderBy(x => x.DisplayOrder)
				.ThenByDescending(x => x.CreatedAtUtc)
				.Select(x => WinnerManagement.ToDto(x))
				.ToListAsync(ct);

			return Results.Ok(new WinnersListResult(true, winners));
		});

		return endpoints;
	}
}


