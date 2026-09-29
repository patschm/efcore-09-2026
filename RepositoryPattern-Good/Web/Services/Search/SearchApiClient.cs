using System.Net.Http.Json;

namespace WebShop.Web.Services.Search;

public sealed class SearchApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<SimilarProductDto>> GetSimilarProductsAsync(int productId, int topN, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<List<SimilarProductDto>>($"/products/{productId}/similar?topN={topN}", cancellationToken) ?? [];
}
