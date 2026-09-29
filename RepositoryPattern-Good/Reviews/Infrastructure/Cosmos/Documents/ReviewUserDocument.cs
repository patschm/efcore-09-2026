using Newtonsoft.Json;

namespace WebShop.Reviews.Infrastructure.Cosmos.Documents;

public sealed class ReviewUserDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "ReviewUser";
    [JsonProperty("reviewUserId")] public int ReviewUserId { get; set; }
    [JsonProperty("name")] public string Name { get; set; } = null!;
    [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)] public string? Email { get; set; }
    [JsonProperty("_etag")] public string? ETag { get; set; }

    public static string BuildId(int reviewUserId) => reviewUserId.ToString();
}
