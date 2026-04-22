namespace MiniApp.Data;

public interface ITicketPurchaseSettingsService
{
	Task<TicketPurchaseSettings> GetSettingsAsync(CancellationToken ct);

	Task<TicketPurchaseSettings> SaveSettingsAsync(int ticketSlotsCount, string? updatedByAdmin, CancellationToken ct);
}

