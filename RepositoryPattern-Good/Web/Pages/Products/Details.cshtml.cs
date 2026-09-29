using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebShop.Web.Identity;
using WebShop.Web.Services.Catalog;
using WebShop.Web.Services.Pricing;
using WebShop.Web.Services.Reviews;
using WebShop.Web.Services.Search;

namespace WebShop.Web.Pages.Products;

public sealed class DetailsModel(
    CatalogApiClient catalogApiClient, PricingApiClient pricingApiClient, ReviewsApiClient reviewsApiClient,
    SearchApiClient searchApiClient, UserManager<ApplicationUser> userManager, JwtTokenService jwtTokenService)
    : PageModel
{
    private const int SimilarProductCount = 10;

    public ProductDto Product { get; private set; } = null!;
    public IReadOnlyList<ProductGroupDto> Ancestors { get; private set; } = [];
    public IReadOnlyList<PriceDto> Prices { get; private set; } = [];
    public IReadOnlyList<ReviewDto> Reviews { get; private set; } = [];
    public IReadOnlyList<SimilarProductViewModel> SimilarProducts { get; private set; } = [];

    [BindProperty]
    public NewReviewInput NewReview { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken) =>
        await LoadAsync(id, cancellationToken) ? Page() : NotFound();

    // Writing a review requires an account - challenging here (rather than hiding the form)
    // covers a request that reaches this handler despite the button being hidden for anonymous
    // visitors, e.g. a stale page or a direct POST.
    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Challenge();

        if (!ModelState.IsValid)
            return await LoadAsync(id, cancellationToken) ? Page() : NotFound();

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        // Matches the "Sterke punten: ...\r\nZwakke punten: ..." shape already found in the
        // legacy scraped review text, so user-submitted and imported reviews read the same way.
        var text = $"Sterke punten: {NewReview.StrongPoints}\r\nZwakke punten: {NewReview.WeakPoints}";

        // The form collects a 1-10 score, matching Review.Score's actual 0-10 domain range -
        // no conversion needed, this is the same scale the review list already displays in.
        var permissionClaims = await userManager.GetClaimsAsync(user);
        var accessToken = jwtTokenService.CreateAccessToken(user, permissionClaims);
        await reviewsApiClient.CreateReviewAsync(id, NewReview.Title, NewReview.Score, text, accessToken, cancellationToken);

        return RedirectToPage(null, null, new { id }, "reviews");
    }

    private async Task<bool> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var product = await catalogApiClient.GetProductByIdAsync(id, cancellationToken);
        if (product is null)
            return false;

        Product = product;
        Ancestors = product.ProductGroupId is { } productGroupId
            ? await catalogApiClient.GetAncestryAsync(productGroupId, cancellationToken)
            : [];

        var pricesTask = pricingApiClient.GetPricesForProductAsync(id, cancellationToken);
        var reviewsTask = reviewsApiClient.GetReviewsForProductAsync(id, cancellationToken);
        var similarTask = searchApiClient.GetSimilarProductsAsync(id, SimilarProductCount, cancellationToken);
        await Task.WhenAll(pricesTask, reviewsTask, similarTask);

        Prices = await pricesTask;
        Reviews = await reviewsTask;
        SimilarProducts = await BuildSimilarProductsAsync(await similarTask, cancellationToken);

        return true;
    }

    // Search only knows product ids and scores - Catalog (name/brand/image) and Pricing (lowest
    // price) are resolved here, each in one batch call, and joined back in Search's own
    // score-descending order (neither Catalog's nor Pricing's response order is guaranteed to
    // match it).
    private async Task<IReadOnlyList<SimilarProductViewModel>> BuildSimilarProductsAsync(
        IReadOnlyList<SimilarProductDto> similar, CancellationToken cancellationToken)
    {
        if (similar.Count == 0)
            return [];

        var productIds = similar.Select(s => s.ProductId).ToList();
        var productsTask = catalogApiClient.GetProductsByIdsAsync(productIds, cancellationToken);
        var lowestPricesTask = pricingApiClient.GetLowestPricesAsync(productIds, cancellationToken);
        await Task.WhenAll(productsTask, lowestPricesTask);

        var productsById = (await productsTask).ToDictionary(p => p.Id);
        var lowestPrices = await lowestPricesTask;

        return similar
            .Where(s => productsById.ContainsKey(s.ProductId))
            .Select(s =>
            {
                var product = productsById[s.ProductId];
                var lowestPrice = lowestPrices.GetValueOrDefault(s.ProductId);
                return new SimilarProductViewModel(
                    product.Id, product.Name, product.BrandName, product.ImageUrl, s.Score,
                    lowestPrice?.Amount, lowestPrice?.Currency);
            })
            .ToList();
    }

    public sealed class NewReviewInput
    {
        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = "";

        [Range(1, 10, ErrorMessage = "Score must be between 1 and 10.")]
        public int Score { get; set; } = 5;

        public string? StrongPoints { get; set; }
        public string? WeakPoints { get; set; }
    }

    public sealed record SimilarProductViewModel(
        int ProductId, string Name, string BrandName, string? ImageUrl, double Score,
        double? LowestPriceAmount, string? LowestPriceCurrency);
}
