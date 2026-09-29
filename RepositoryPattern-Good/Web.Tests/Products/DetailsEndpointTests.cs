using System.Net;
using WebShop.Web.Services.Catalog;
using WebShop.Web.Services.Pricing;
using WebShop.Web.Services.Reviews;
using WebShop.Web.Services.Search;
using WebShop.Web.Tests.Fakes;

namespace WebShop.Web.Tests.Products;

public sealed class DetailsEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public DetailsEndpointTests(WebApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Details_page_resolves_and_orders_similar_products_via_catalog_and_pricing()
    {
        var client = _factory.CreateClient();

        _factory.CatalogHandler.OnRequest = request => request switch
        {
            { Method.Method: "GET" } when request.RequestUri!.AbsolutePath == "/products/42" =>
                FakeResponses.Json(HttpStatusCode.OK, new ProductDto(42, "Widget", 7, "Acme", null, null, [])),
            { Method.Method: "POST" } when request.RequestUri!.AbsolutePath == "/products/by-ids" =>
                // Product 102 (returned by Search below) is deliberately absent here - simulates
                // a since-deleted product, which BuildSimilarProductsAsync must filter out rather
                // than blow up on.
                FakeResponses.Json(HttpStatusCode.OK, new List<ProductSummaryDto> { new(101, "Gadget", 3, "Acme", "/img/gadget.jpg") }),
            _ => throw new InvalidOperationException($"Unexpected catalog request: {request.Method} {request.RequestUri}")
        };

        _factory.PricingHandler.OnRequest = request => request switch
        {
            { Method.Method: "GET" } when request.RequestUri!.AbsolutePath == "/prices" =>
                FakeResponses.Json(HttpStatusCode.OK, new List<PriceDto>()),
            { Method.Method: "POST" } when request.RequestUri!.AbsolutePath == "/prices/lowest" =>
                FakeResponses.Json(HttpStatusCode.OK, new Dictionary<int, LowestPriceDto> { [101] = new(19.99, "EUR") }),
            _ => throw new InvalidOperationException($"Unexpected pricing request: {request.Method} {request.RequestUri}")
        };

        _factory.ReviewsHandler.OnRequest = request => request.RequestUri!.AbsolutePath == "/reviews"
            ? FakeResponses.Json(HttpStatusCode.OK, new List<ReviewDto>())
            : throw new InvalidOperationException($"Unexpected reviews request: {request.RequestUri}");

        _factory.SearchHandler.OnRequest = request => request.RequestUri!.AbsolutePath == "/products/42/similar"
            ? FakeResponses.Json(HttpStatusCode.OK, new List<SimilarProductDto> { new(101, 0.9), new(102, 0.5) })
            : throw new InvalidOperationException($"Unexpected search request: {request.RequestUri}");

        var response = await client.GetAsync("/products/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Similar products (1)", html);
        Assert.Contains("Gadget", html);
        // Details.cshtml formats this with "0.00" against the current culture, not
        // CultureInfo.InvariantCulture - matching that here rather than assuming "." as the
        // decimal separator, which would fail on a machine whose culture uses ",".
        Assert.Contains(19.99.ToString("0.00"), html);
        Assert.Contains("90%", html);
    }

    [Fact]
    public async Task Details_returns_not_found_for_an_unknown_product()
    {
        var client = _factory.CreateClient();
        _factory.CatalogHandler.OnRequest = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        var response = await client.GetAsync("/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
