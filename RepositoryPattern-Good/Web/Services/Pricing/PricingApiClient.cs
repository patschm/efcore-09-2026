using System.Net.Http.Json;

namespace WebShop.Web.Services.Pricing;

public sealed class PricingApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<PriceDto>> GetPricesForProductAsync(int productId, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<List<PriceDto>>($"/prices?productId={productId}", cancellationToken) ?? [];

    // Empty input short-circuits rather than hitting the API with an empty list. A product with
    // no prices at all is simply absent from the returned dictionary.
    public async Task<IReadOnlyDictionary<int, LowestPriceDto>> GetLowestPricesAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return new Dictionary<int, LowestPriceDto>();

        var response = await httpClient.PostAsJsonAsync("/prices/lowest", new { productIds }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Dictionary<int, LowestPriceDto>>(cancellationToken) ?? new Dictionary<int, LowestPriceDto>();
    }
}
