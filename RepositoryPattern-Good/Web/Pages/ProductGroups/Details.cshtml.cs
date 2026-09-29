using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebShop.Web.Services.Catalog;
using WebShop.Web.Services.Reviews;

namespace WebShop.Web.Pages.ProductGroups;

public sealed class DetailsModel(CatalogApiClient catalogApiClient, ReviewsApiClient reviewsApiClient) : PageModel
{
    private const int PageSize = 20;

    public ProductGroupDto Group { get; private set; } = null!;
    public IReadOnlyList<ProductGroupDto> Ancestors { get; private set; } = [];
    public IReadOnlyList<ProductGroupDto> ChildGroups { get; private set; } = [];
    public IReadOnlyList<ProductSummaryDto> Products { get; private set; } = [];
    public IReadOnlyDictionary<int, double> AverageScoresByProductId { get; private set; } = new Dictionary<int, double>();
    public int CurrentPage { get; private set; } = 1;
    public int TotalPages { get; private set; }

    // First page, 2 pages either side of the current one, and the last page - null marks a gap
    // between two non-adjacent numbers, rendered as "...".
    public IReadOnlyList<int?> PageNumbersToDisplay
    {
        get
        {
            if (TotalPages <= 1)
                return [];

            var pages = new SortedSet<int> { 1, TotalPages };
            for (var p = CurrentPage - 2; p <= CurrentPage + 2; p++)
            {
                if (p >= 1 && p <= TotalPages)
                    pages.Add(p);
            }

            var result = new List<int?>();
            var previous = 0;
            foreach (var p in pages)
            {
                if (previous != 0 && p - previous > 1)
                    result.Add(null);
                result.Add(p);
                previous = p;
            }

            return result;
        }
    }

    // Named "pageNumber", not "page" - Razor Pages reserves "page" as a route value identifying
    // which page to route to, so a query parameter of that name gets silently dropped by the
    // asp-route-* tag helper instead of appearing in the generated URL.
    public async Task<IActionResult> OnGetAsync(int id, int pageNumber, CancellationToken cancellationToken)
    {
        var group = await catalogApiClient.GetProductGroupByIdAsync(id, cancellationToken);
        if (group is null)
            return NotFound();

        Group = group;
        Ancestors = await catalogApiClient.GetAncestryAsync(id, cancellationToken);
        ChildGroups = await catalogApiClient.GetProductGroupsAsync(id, cancellationToken);

        // Leaf group (no sub-groups) - show its products instead.
        if (ChildGroups.Count == 0)
        {
            CurrentPage = pageNumber < 1 ? 1 : pageNumber;
            var result = await catalogApiClient.GetProductsByGroupAsync(id, CurrentPage, PageSize, cancellationToken);
            Products = result.Items;
            TotalPages = result.TotalPages;
            AverageScoresByProductId = await reviewsApiClient.GetAverageScoresAsync(Products.Select(p => p.Id).ToList(), cancellationToken);
        }

        return Page();
    }
}
