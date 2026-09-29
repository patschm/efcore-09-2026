using Newtonsoft.Json;

namespace WebShop.Catalog.Infrastructure.Cosmos.Documents;

public sealed class BrandDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "Brand";
    [JsonProperty("brandId")] public int BrandId { get; set; }
    [JsonProperty("name")] public string Name { get; set; } = null!;
    [JsonProperty("website", NullValueHandling = NullValueHandling.Ignore)] public string? Website { get; set; }
    [JsonProperty("logo", NullValueHandling = NullValueHandling.Ignore)] public string? Logo { get; set; }
    [JsonProperty("_etag")] public string? ETag { get; set; }

    public static string BuildId(int brandId) => brandId.ToString();
}
