using Newtonsoft.Json;

namespace WebShop.Catalog.Infrastructure.Cosmos.Documents;

// One item per ProductSpecificationValue, in the same partition as its Product - split out
// (rather than embedded in ProductDocument) so a spec sheet of any size never risks the
// per-item size cap, and so setting one value doesn't require rewriting the whole product.
// Key/name/unit are denormalized from the owning SpecificationDefinition so rendering never
// needs to join back to the reference container's ProductGroup document.
public sealed class SpecValueDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "SpecValue";
    [JsonProperty("productId")] public int ProductId { get; set; }
    [JsonProperty("specValueId")] public int SpecValueId { get; set; }
    [JsonProperty("specDefId")] public int SpecDefId { get; set; }
    [JsonProperty("key")] public string Key { get; set; } = null!;
    [JsonProperty("name")] public string Name { get; set; } = null!;
    [JsonProperty("unit", NullValueHandling = NullValueHandling.Ignore)] public string? Unit { get; set; }
    [JsonProperty("numberValue", NullValueHandling = NullValueHandling.Ignore)] public decimal? NumberValue { get; set; }
    [JsonProperty("stringValue", NullValueHandling = NullValueHandling.Ignore)] public string? StringValue { get; set; }
    [JsonProperty("boolValue", NullValueHandling = NullValueHandling.Ignore)] public bool? BoolValue { get; set; }

    // Keyed by the value's own id, not by specDefId - a SpecificationDefinition with
    // Multiple=true can legitimately hold several distinct values for one product (e.g.
    // "supported audio formats: MP3, WMA"), each with its own ProductSpecificationValueId.
    // Keying by specDefId would collapse those into one item, silently keeping only whichever
    // value was written last.
    public static string BuildId(int specValueId) => $"spec|{specValueId}";
}
