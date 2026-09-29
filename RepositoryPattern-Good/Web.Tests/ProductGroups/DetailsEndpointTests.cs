using System.Net;
using WebShop.Web.Services.Catalog;
using WebShop.Web.Tests.Fakes;

namespace WebShop.Web.Tests.ProductGroups;

public sealed class DetailsEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public DetailsEndpointTests(WebApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Leaf_group_lists_its_products_with_average_scores()
    {
        var client = _factory.CreateClient();

        _factory.CatalogHandler.OnRequest = request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            var query = request.RequestUri.Query;
            return (path, query) switch
            {
                ("/product-groups/5", _) => FakeResponses.Json(HttpStatusCode.OK, new ProductGroupDto(5, "Televisions", null, null)),
                ("/product-groups", var q) when q.Contains("parentId=5") => FakeResponses.Json(HttpStatusCode.OK, new List<ProductGroupDto>()),
                ("/products", _) => FakeResponses.Json(HttpStatusCode.OK,
                    new PagedResult<ProductSummaryDto>([new ProductSummaryDto(200, "TV Model X", 3, "Sony", null)], 1, 1, 20)),
                _ => throw new InvalidOperationException($"Unexpected catalog request: {request.Method} {request.RequestUri}")
            };
        };

        _factory.ReviewsHandler.OnRequest = request => request.RequestUri!.AbsolutePath == "/reviews/average-scores"
            ? FakeResponses.Json(HttpStatusCode.OK, new Dictionary<int, double> { [200] = 8.0 })
            : throw new InvalidOperationException($"Unexpected reviews request: {request.RequestUri}");

        var response = await client.GetAsync("/product-groups/5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("TV Model X", html);
        Assert.Contains("Sony", html);
    }

    [Fact]
    public async Task Group_with_sub_groups_lists_the_sub_groups_instead_of_products()
    {
        var client = _factory.CreateClient();

        _factory.CatalogHandler.OnRequest = request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            var query = request.RequestUri.Query;
            return (path, query) switch
            {
                ("/product-groups/9", _) => FakeResponses.Json(HttpStatusCode.OK, new ProductGroupDto(9, "Electronics", null, null)),
                ("/product-groups", var q) when q.Contains("parentId=9") => FakeResponses.Json(HttpStatusCode.OK, new List<ProductGroupDto>
                {
                    new(10, "TVs", 9, null),
                    new(11, "Audio", 9, null)
                }),
                _ => throw new InvalidOperationException($"Unexpected catalog request: {request.Method} {request.RequestUri}")
            };
        };

        // Neither /products nor /reviews/average-scores should ever be called for a non-leaf
        // group - leaving both handlers unset means any such call fails the test loudly.

        var response = await client.GetAsync("/product-groups/9");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("TVs", html);
        Assert.Contains("Audio", html);
    }
}
