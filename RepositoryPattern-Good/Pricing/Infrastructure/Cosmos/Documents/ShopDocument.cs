using Newtonsoft.Json;

namespace WebShop.Pricing.Infrastructure.Cosmos.Documents;

public sealed class ShopDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "Shop";
    [JsonProperty("shopId")] public int ShopId { get; set; }
    [JsonProperty("name")] public string Name { get; set; } = null!;
    [JsonProperty("url")] public string Url { get; set; } = null!;
    [JsonProperty("logo", NullValueHandling = NullValueHandling.Ignore)] public string? Logo { get; set; }
    [JsonProperty("rating")] public double Rating { get; set; }
    [JsonProperty("_etag")] public string? ETag { get; set; }

    public static string BuildId(int shopId) => shopId.ToString();
}
