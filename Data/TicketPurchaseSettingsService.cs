using Microsoft.EntityFrameworkCore;

namespace MiniApp.Data;

public sealed class TicketPurchaseSettingsService : ITicketPurchaseSettingsService
{
	private const int DefaultTicketSlotsCount = 10;
	private readonly AppDbContext _db;

	public TicketPurchaseSettingsService(AppDbContext db)
	{
		_db = db;
	}

	public async Task<TicketPurchaseSettings> GetSettingsAsync(CancellationToken ct)
	{
		var settings = await _db.TicketPurchaseSettings.SingleOrDefaultAsync(x => x.Id == 1, ct);
		if (settings is not null)
			return settings;

		settings = new TicketPurchaseSettings
		{
			TicketSlotsCount = DefaultTicketSlotsCount,
			UpdatedAtUtc = DateTimeOffset.UtcNow
		};

		_db.TicketPurchaseSettings.Add(settings);
		await _db.SaveChangesAsync(ct);
		return settings;
	}

	public async Task<TicketPurchaseSettings> SaveSettingsAsync(int ticketSlotsCount, string? updatedByAdmin, CancellationToken ct)
	{
		var settings = await GetSettingsAsync(ct);
		settings.TicketSlotsCount = Math.Clamp(ticketSlotsCount, 1, 50);
		settings.UpdatedByAdmin = string.IsNullOrWhiteSpace(updatedByAdmin) ? "admin" : updatedByAdmin.Trim();
		settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
		await _db.SaveChangesAsync(ct);
		return settings;
	}
}

