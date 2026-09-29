using Newtonsoft.Json;

namespace WebShop.Pricing.Infrastructure.Cosmos.Documents;

// Lives in the products container, partitioned by productId alongside its Catalog Product,
// its Reviews, and its Search Embedding - one shop's quote per product, id'd by shopId (the
// domain enforces at most one Price per (ShopId, ProductId) pair). Shop's name/logo/rating are
// denormalized so rendering a price list never needs to look Shop up separately.
public sealed class PriceDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "Price";
    [JsonProperty("productId")] public int ProductId { get; set; }
    [JsonProperty("priceId")] public int PriceId { get; set; }
    [JsonProperty("shopId")] public int ShopId { get; set; }
    [JsonProperty("shopName", NullValueHandling = NullValueHandling.Ignore)] public string? ShopName { get; set; }
    [JsonProperty("shopLogo", NullValueHandling = NullValueHandling.Ignore)] public string? ShopLogo { get; set; }
    [JsonProperty("shopRating")] public double? ShopRating { get; set; }
    [JsonProperty("shopPrice")] public MoneyDocument ShopPrice { get; set; } = null!;
    [JsonProperty("shippingPrice")] public MoneyDocument ShippingPrice { get; set; } = null!;
    [JsonProperty("inStock")] public int InStock { get; set; }
    [JsonProperty("_etag")] public string? ETag { get; set; }

    public static string BuildId(int shopId) => $"price|{shopId}";
}
