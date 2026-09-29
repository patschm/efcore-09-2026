using System.Net;
using System.Net.Http.Json;

namespace WebShop.Search.Api.Tests;

public class SearchEndpointsTests : IDisposable
{
    private readonly SearchApiFactory _factory = new();
    private readonly HttpClient _client;

    public SearchEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Upsert_embedding_returns_204()
    {
        var response = await _client.PostAsJsonAsync("/embeddings", new { productId = 1, content = "Samsung QLED 55 inch smart tv" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Search_returns_previously_upserted_products()
    {
        await _client.PostAsJsonAsync("/embeddings", new { productId = 1, content = "Samsung QLED 55 inch smart tv" });
        await _client.PostAsJsonAsync("/embeddings", new { productId = 2, content = "Apple iPhone 15 pro smartphone" });

        var results = await _client.GetFromJsonAsync<List<SearchResultResponse>>("/search?text=smart%20tv&topN=10");

        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task Similar_excludes_the_source_product_itself()
    {
        await _client.PostAsJsonAsync("/embeddings", new { productId = 1, content = "Samsung QLED 55 inch smart tv" });
        await _client.PostAsJsonAsync("/embeddings", new { productId = 2, content = "Samsung QLED 65 inch smart tv" });

        var results = await _client.GetFromJsonAsync<List<SearchResultResponse>>("/products/1/similar?topN=10");

        Assert.NotNull(results);
        Assert.DoesNotContain(results, r => r.ProductId == 1);
        Assert.Contains(results, r => r.ProductId == 2);
    }

    private sealed record SearchResultResponse(int ProductId, double Score);
}
