using Newtonsoft.Json;

namespace WebShop.Search.Infrastructure.Cosmos.Documents;

// Lives in the products container, in the same partition as its Catalog Product - split out
// as its own item (rather than embedded in the Product document) so Search's re-embedding
// writes never collide with Catalog's product-field writes, and so routine product reads
// don't pay the ~10 KB of JSON a 1024-float vector serializes to.
public sealed class EmbeddingDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "Embedding";
    [JsonProperty("productId")] public int ProductId { get; set; }
    [JsonProperty("vector")] public double[] Vector { get; set; } = [];
    [JsonProperty("contentHash")] public byte[] ContentHash { get; set; } = [];
    [JsonProperty("generatedAt")] public DateTime GeneratedAt { get; set; }
    [JsonProperty("_etag")] public string? ETag { get; set; }

    public static string BuildId(int productId) => $"embedding|{productId}";
}
