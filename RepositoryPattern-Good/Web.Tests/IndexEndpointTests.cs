using System.Net;
using WebShop.Web.Services.Catalog;
using WebShop.Web.Tests.Fakes;

namespace WebShop.Web.Tests;

public sealed class IndexEndpointTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public IndexEndpointTests(WebApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Home_page_lists_the_top_level_product_groups()
    {
        var client = _factory.CreateClient();

        _factory.CatalogHandler.OnRequest = request => request.RequestUri!.AbsolutePath == "/product-groups"
            ? FakeResponses.Json(HttpStatusCode.OK, new List<ProductGroupDto> { new(1, "Televisions", null, null), new(2, "Audio", null, null) })
            : throw new InvalidOperationException($"Unexpected catalog request: {request.RequestUri}");

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Televisions", html);
        Assert.Contains("Audio", html);
    }
}
