using Newtonsoft.Json;

namespace WebShop.Catalog.Infrastructure.Cosmos.Documents;

public sealed class ProductGroupDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "ProductGroup";
    [JsonProperty("productGroupId")] public int ProductGroupId { get; set; }
    [JsonProperty("name")] public string Name { get; set; } = null!;
    [JsonProperty("imageUrl", NullValueHandling = NullValueHandling.Ignore)] public string? ImageUrl { get; set; }

    // Absent (not merely null) for a top-level group - GetByParentId(null) relies on
    // NOT IS_DEFINED(c.parentId), which only works when the property is omitted entirely.
    [JsonProperty("parentId", NullValueHandling = NullValueHandling.Ignore)] public int? ParentId { get; set; }

    [JsonProperty("specificationDefinitions")] public List<SpecificationDefinitionDocument> SpecificationDefinitions { get; set; } = [];
    [JsonProperty("_etag")] public string? ETag { get; set; }

    public static string BuildId(int productGroupId) => productGroupId.ToString();
}

public sealed class SpecificationDefinitionDocument
{
    [JsonProperty("specificationDefinitionId")] public int SpecificationDefinitionId { get; set; }
    [JsonProperty("key")] public string Key { get; set; } = null!;
    [JsonProperty("name")] public string Name { get; set; } = null!;
    [JsonProperty("unit", NullValueHandling = NullValueHandling.Ignore)] public string? Unit { get; set; }
    [JsonProperty("valueType", NullValueHandling = NullValueHandling.Ignore)] public string? ValueType { get; set; }
    [JsonProperty("multiple")] public bool Multiple { get; set; }
    [JsonProperty("explanation", NullValueHandling = NullValueHandling.Ignore)] public string? Explanation { get; set; }
}
