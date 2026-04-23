using Microsoft.EntityFrameworkCore;
using MiniApp.Data;
using Npgsql;

namespace MiniApp.Features.Winners;

public static class WinnersEndpoints
{
	public static IEndpointRouteBuilder MapWinnersEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapGet("/api/winners", async (AppDbContext db, ILoggerFactory loggerFactory, CancellationToken ct) =>
		{
			try
			{
				var winners = await db.Set<WinnerEntry>()
					.AsNoTracking()
					.Where(x => x.IsPublished)
					.OrderBy(x => x.DisplayOrder)
					.ThenByDescending(x => x.CreatedAtUtc)
					.Select(x => WinnerManagement.ToDto(x))
					.ToListAsync(ct);

				return Results.Ok(new WinnersListResult(true, winners));
			}
			catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
			{
				loggerFactory.CreateLogger("WinnersEndpoints")
					.LogWarning(ex, "winner_entries table is missing. Returning an empty winners list until migrations are applied.");
				return Results.Ok(new WinnersListResult(true, Array.Empty<WinnerEntryDto>()));
			}
		});

		return endpoints;
	}
}


