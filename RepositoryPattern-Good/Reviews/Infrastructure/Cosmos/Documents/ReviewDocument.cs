using Newtonsoft.Json;

namespace WebShop.Reviews.Infrastructure.Cosmos.Documents;

// Lives in the products container, partitioned by productId. Reviewer name/email are
// denormalized from ReviewUser (still persisted independently in the reference container -
// IReviewUserRepository is a real, used contract) so rendering a review never needs a second
// lookup. IsDeleted/DeletedAt/Ttl exist only here: Remove is the one repository method in this
// whole design that actually deletes anything today (see SoftDelete).
public sealed class ReviewDocument
{
    [JsonProperty("id")] public string Id { get; set; } = null!;
    [JsonProperty("type")] public string Type { get; set; } = "Review";
    [JsonProperty("productId")] public int ProductId { get; set; }
    [JsonProperty("reviewId")] public int ReviewId { get; set; }
    [JsonProperty("reviewType")] public string ReviewType { get; set; } = null!;
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)] public string? Title { get; set; }
    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)] public string? Text { get; set; }
    [JsonProperty("score", NullValueHandling = NullValueHandling.Ignore)] public decimal? Score { get; set; }
    [JsonProperty("reviewUserId", NullValueHandling = NullValueHandling.Ignore)] public int? ReviewUserId { get; set; }
    [JsonProperty("reviewer", NullValueHandling = NullValueHandling.Ignore)] public ReviewerSnapshot? Reviewer { get; set; }
    [JsonProperty("creationDate")] public string CreationDate { get; set; } = null!;
    [JsonProperty("isDeleted")] public bool IsDeleted { get; set; }
    [JsonProperty("deletedAt", NullValueHandling = NullValueHandling.Ignore)] public DateTime? DeletedAt { get; set; }
    [JsonProperty("ttl", NullValueHandling = NullValueHandling.Ignore)] public int? Ttl { get; set; }
    [JsonProperty("_etag")] public string? ETag { get; set; }

    public static string BuildId(int reviewId) => $"review|{reviewId}";
}

public sealed class ReviewerSnapshot
{
    [JsonProperty("name")] public string Name { get; set; } = null!;
    [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)] public string? Email { get; set; }
}
