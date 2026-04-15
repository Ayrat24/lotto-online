using Microsoft.AspNetCore.Authorization;
using MiniApp.Admin;
using MiniApp.Features.Localization;

namespace MiniApp.Pages.Admin;

[Authorize(Policy = AdminAuth.PolicyName)]
public sealed class IndexModel : LocalizedAdminPageModel
{
    public IndexModel(ILocalizationService localization) : base(localization)
    {
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadUiTextAsync(ct);
    }
}

