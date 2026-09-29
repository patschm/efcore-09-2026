using Microsoft.AspNetCore.Mvc.RazorPages;
using WebShop.Web.Services.Catalog;

namespace WebShop.Web.Pages;

public sealed class IndexModel(CatalogApiClient catalogApiClient) : PageModel
{
    public IReadOnlyList<ProductGroupDto> TopLevelProductGroups { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        TopLevelProductGroups = await catalogApiClient.GetProductGroupsAsync(parentId: null, cancellationToken);
    }
}
