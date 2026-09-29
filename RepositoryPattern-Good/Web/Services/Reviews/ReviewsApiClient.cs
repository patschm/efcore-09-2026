using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebShop.Web.Services.Reviews;

public sealed class ReviewsApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<ReviewDto>> GetReviewsForProductAsync(int productId, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<List<ReviewDto>>($"/reviews?productId={productId}", cancellationToken) ?? [];

    // ReviewUserId is not part of the request body - Reviews.Api derives it from accessToken's
    // claims instead, so a caller can never submit a review under someone else's identity.
    // The review itself uses a random high id - this BFF has no sequence of its own, and legacy
    // scraped review ids are all far below this range, so collisions are practically impossible.
    public async Task CreateReviewAsync(int productId, string title, decimal score, string text, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/reviews")
        {
            Content = JsonContent.Create(new
            {
                id = Random.Shared.Next(1_000_000_000, int.MaxValue),
                productId,
                type = "UserReview",
                creationDate = DateOnly.FromDateTime(DateTime.UtcNow),
                title,
                text,
                score
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // Called once, right after WebShop.Web creates the ApplicationUser - links a fresh Identity
    // account to a matching ReviewUser in the Reviews context, which knows the reviewer only as
    // a name/email, never as a login credential.
    public async Task CreateReviewUserAsync(int id, string name, string? email, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/review-users")
        {
            Content = JsonContent.Create(new { id, name, email })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // Empty input short-circuits rather than hitting the API with an empty list.
    public async Task<IReadOnlyDictionary<int, double>> GetAverageScoresAsync(IReadOnlyCollection<int> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return new Dictionary<int, double>();

        var response = await httpClient.PostAsJsonAsync("/reviews/average-scores", new { productIds }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Dictionary<int, double>>(cancellationToken) ?? new Dictionary<int, double>();
    }
}
