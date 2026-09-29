using System.Net;
using System.Net.Http.Json;

namespace WebShop.Web.Services.Catalog;

// Talks to the Catalog API purely over HTTP, the same as any other client would - this BFF never
// references Catalog's Domain/Application assemblies directly, to keep the bounded context
// boundary real rather than just aspirational.
public sealed class CatalogApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ProductGroupDto>> GetProductGroupsAsync(int? parentId, CancellationToken cancellationToken)
    {
        var url = parentId.HasValue ? $"/product-groups?parentId={parentId.Value}" : "/product-groups";
        return await httpClient.GetFromJsonAsync<List<ProductGroupDto>>(url, cancellationToken) ?? [];
    }

    public async Task<ProductGroupDto?> GetProductGroupByIdAsync(int id, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/product-groups/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductGroupDto>(cancellationToken);
    }

    // Walks the ParentId chain up to the root, for breadcrumbs - returns root-first (the
    // group passed in is last), since there's no single endpoint that returns a group's
    // full ancestry.
    public async Task<IReadOnlyList<ProductGroupDto>> GetAncestryAsync(int productGroupId, CancellationToken cancellationToken)
    {
        var path = new List<ProductGroupDto>();
        int? currentId = productGroupId;
        while (currentId is not null)
        {
            var group = await GetProductGroupByIdAsync(currentId.Value, cancellationToken);
            if (group is null)
                break;

            path.Add(group);
            currentId = group.ParentId;
        }

        path.Reverse();
        return path;
    }

    public async Task<PagedResult<ProductSummaryDto>> GetProductsByGroupAsync(
        int productGroupId, int page, int pageSize, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<PagedResult<ProductSummaryDto>>(
            $"/products?productGroupId={productGroupId}&page={page}&pageSize={pageSize}", cancellationToken)
        ?? new PagedResult<ProductSummaryDto>([], 0, page, pageSize);

    public async Task<ProductDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/products/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken);
    }

    // Empty input short-circuits rather than hitting the API with an empty list.
    public async Task<IReadOnlyList<ProductSummaryDto>> GetProductsByIdsAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return [];

        var response = await httpClient.PostAsJsonAsync("/products/by-ids", new { productIds }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ProductSummaryDto>>(cancellationToken) ?? [];
    }
}
