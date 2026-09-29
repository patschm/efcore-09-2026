using Newtonsoft.Json;

namespace WebShop.Catalog.Infrastructure.Cosmos.Documents;

public sealed class ProductDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "Product";
    [JsonProperty("productId")] public int ProductId { get; set; }
    [JsonProperty("name")] public string Name { get; set; } = null!;
    [JsonProperty("brandId")] public int BrandId { get; set; }
    [JsonProperty("brandName", NullValueHandling = NullValueHandling.Ignore)] public string? BrandName { get; set; }
    [JsonProperty("productGroupId", NullValueHandling = NullValueHandling.Ignore)] public int? ProductGroupId { get; set; }
    [JsonProperty("groupPath")] public List<GroupPathEntry> GroupPath { get; set; } = [];
    [JsonProperty("imageUrl", NullValueHandling = NullValueHandling.Ignore)] public string? ImageUrl { get; set; }
    [JsonProperty("_etag")] public string? ETag { get; set; }

    public static string BuildId(int productId) => $"product|{productId}";
}

public sealed class GroupPathEntry
{
    [JsonProperty("id")] public int Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; } = null!;
}
