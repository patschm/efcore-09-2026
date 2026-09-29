using System.Net;
using System.Net.Http.Json;

namespace WebShop.Catalog.Api.Tests;

public class CatalogEndpointsTests : IDisposable
{
    private readonly CatalogApiFactory _factory = new();
    private readonly HttpClient _client;

    public CatalogEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Create_brand_returns_201()
    {
        var response = await _client.PostAsJsonAsync("/brands", new { id = 1, name = "Samsung", website = "https://www.samsung.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_product_with_empty_name_returns_400_via_the_domain_exception_handler()
    {
        await _client.PostAsJsonAsync("/brands", new { id = 1, name = "Samsung" });

        var response = await _client.PostAsJsonAsync("/products", new { id = 1, name = "", brandId = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Product name is required", body);
    }

    [Fact]
    public async Task Define_duplicate_specification_key_returns_400_with_the_domain_error()
    {
        await _client.PostAsJsonAsync("/product-groups", new { id = 1, name = "Televisions" });
        await _client.PostAsJsonAsync("/product-groups/1/specifications", new { id = 1, key = "screen_size", name = "Screen size" });

        var response = await _client.PostAsJsonAsync("/product-groups/1/specifications", new { id = 2, key = "screen_size", name = "dup" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("already defined", body);
    }

    [Fact]
    public async Task Set_specification_value_with_two_shapes_at_once_returns_400()
    {
        await SeedProductAsync();

        var response = await _client.PutAsJsonAsync(
            "/products/1/specification-values/1", new { id = 1, number = 10, text = "ten" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Set_specification_value_for_a_missing_product_returns_400()
    {
        var response = await _client.PutAsJsonAsync(
            "/products/999/specification-values/1", new { id = 1, number = 10 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_product_round_trips_the_specification_value_just_set()
    {
        await SeedProductAsync();
        await _client.PutAsJsonAsync("/products/1/specification-values/1", new { id = 1, number = 55 });

        var product = await _client.GetFromJsonAsync<ProductResponse>("/products/1");

        Assert.NotNull(product);
        Assert.Equal("Samsung QLED 55\"", product.Name);
        Assert.Equal("Samsung", product.BrandName);
        var specification = Assert.Single(product.Specifications);
        Assert.Equal("Screen size", specification.Name);
        Assert.Equal(new[] { "55" }, specification.Values);
    }

    [Fact]
    public async Task Get_missing_product_returns_404()
    {
        var response = await _client.GetAsync("/products/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_product_groups_without_a_parent_returns_only_top_level_groups()
    {
        await _client.PostAsJsonAsync("/product-groups", new { id = 1, name = "Electronics" });
        await _client.PostAsJsonAsync("/product-groups", new { id = 2, name = "Televisions", parentId = 1 });

        var groups = await _client.GetFromJsonAsync<List<ProductGroupResponse>>("/product-groups");

        Assert.NotNull(groups);
        var group = Assert.Single(groups);
        Assert.Equal(1, group.Id);
    }

    [Fact]
    public async Task List_product_groups_with_a_parent_returns_only_its_children()
    {
        await _client.PostAsJsonAsync("/product-groups", new { id = 1, name = "Electronics" });
        await _client.PostAsJsonAsync("/product-groups", new { id = 2, name = "Televisions", parentId = 1 });

        var children = await _client.GetFromJsonAsync<List<ProductGroupResponse>>("/product-groups?parentId=1");

        Assert.NotNull(children);
        var child = Assert.Single(children);
        Assert.Equal(2, child.Id);
    }

    [Fact]
    public async Task Get_missing_product_group_returns_404()
    {
        var response = await _client.GetAsync("/product-groups/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_products_by_group_returns_only_products_in_that_group()
    {
        await SeedProductAsync();
        await _client.PostAsJsonAsync("/product-groups", new { id = 2, name = "Other" });
        await _client.PostAsJsonAsync("/products", new { id = 2, name = "Other product", brandId = 1, productGroupId = 2 });

        var result = await _client.GetFromJsonAsync<PagedProductsResponse>("/products?productGroupId=1");

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        var product = Assert.Single(result.Items);
        Assert.Equal(1, product.Id);
        Assert.Equal("Samsung", product.BrandName);
    }

    [Fact]
    public async Task List_products_by_group_pages_the_results()
    {
        await SeedProductAsync();
        for (var id = 2; id <= 5; id++)
            await _client.PostAsJsonAsync("/products", new { id, name = $"Product {id}", brandId = 1, productGroupId = 1 });

        var firstPage = await _client.GetFromJsonAsync<PagedProductsResponse>("/products?productGroupId=1&page=1&pageSize=2");
        var secondPage = await _client.GetFromJsonAsync<PagedProductsResponse>("/products?productGroupId=1&page=2&pageSize=2");

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(5, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);
        Assert.DoesNotContain(firstPage.Items, p => secondPage.Items.Any(sp => sp.Id == p.Id));
    }

    [Fact]
    public async Task Get_products_by_ids_returns_only_the_requested_products()
    {
        await SeedProductAsync();
        await _client.PostAsJsonAsync("/products", new { id = 2, name = "Other product", brandId = 1, productGroupId = 1 });

        var response = await _client.PostAsJsonAsync("/products/by-ids", new { productIds = new[] { 1 } });
        var products = await response.Content.ReadFromJsonAsync<List<ProductSummaryResponse>>();

        Assert.NotNull(products);
        var product = Assert.Single(products);
        Assert.Equal(1, product.Id);
        Assert.Equal("Samsung", product.BrandName);
    }

    private async Task SeedProductAsync()
    {
        await _client.PostAsJsonAsync("/brands", new { id = 1, name = "Samsung" });
        await _client.PostAsJsonAsync("/product-groups", new { id = 1, name = "Televisions" });
        await _client.PostAsJsonAsync("/product-groups/1/specifications", new { id = 1, key = "screen_size", name = "Screen size" });
        await _client.PostAsJsonAsync("/products", new { id = 1, name = "Samsung QLED 55\"", brandId = 1, productGroupId = 1 });
    }

    private sealed record ProductResponse(int Id, string Name, string BrandName, IReadOnlyList<SpecificationResponse> Specifications);

    private sealed record SpecificationResponse(
        int SpecificationDefinitionId, string Name, string? Type, string? Unit, bool Multiple, string? Explanation, IReadOnlyList<string> Values);

    private sealed record ProductGroupResponse(int Id, string Name, int? ParentId, string? ImageUrl);

    private sealed record ProductSummaryResponse(int Id, string Name, int BrandId, string BrandName, string? ImageUrl);

    private sealed record PagedProductsResponse(IReadOnlyList<ProductSummaryResponse> Items, int TotalCount, int Page, int PageSize);
}
