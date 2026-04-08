using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MiniApp.Admin;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class LocalizationModel : PageModel
{
    private readonly ILocalizationService _localization;

    public LocalizationModel(ILocalizationService localization)
    {
        _localization = localization;
    }

    public IReadOnlyList<LocalizationAdminItem> Items { get; private set; } = Array.Empty<LocalizationAdminItem>();

    [TempData]
    public string? Toast { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Items = await _localization.GetAdminItemsAsync(ct);
    }

    public async Task<IActionResult> OnPostSaveAsync(string key, string englishValue, string russianValue, string uzbekValue, CancellationToken ct)
    {
        var result = await _localization.UpsertAsync(new LocalizationAdminUpdateRequest(
            key,
            englishValue,
            russianValue,
            uzbekValue), ct);

        Toast = result.Ok ? $"Saved '{key}'." : result.Error;
        return RedirectToPage();
    }
}

