using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebShop.Reviews.Api.Tests;

public class ReviewsEndpointsTests : IDisposable
{
    private readonly ReviewsApiFactory _factory = new();
    private readonly HttpClient _client;

    public ReviewsEndpointsTests()
    {
        _client = _factory.CreateClient();
        // Authenticated, but with no linked ReviewUser - matches most of these tests, which
        // don't care about reviewer identity. Tests that do (round-trip) issue their own token.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken());
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Create_review_user_returns_201()
    {
        var response = await _client.PostAsJsonAsync("/review-users", new { id = 1, name = "Alice" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_review_without_a_review_user_returns_201()
    {
        var response = await _client.PostAsJsonAsync(
            "/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 4.5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_review_without_a_token_returns_401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync(
            "/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 4.5 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_review_user_without_a_token_returns_401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/review-users", new { id = 1, name = "Alice" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_review_with_a_token_lacking_the_create_permission_returns_403()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(permissions: []));

        var response = await _client.PostAsJsonAsync(
            "/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 4.5 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_review_without_the_administer_permission_returns_403()
    {
        await _client.PostAsJsonAsync(
            "/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 4.5 });

        var response = await _client.DeleteAsync("/reviews/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_review_with_the_administer_permission_removes_it()
    {
        await _client.PostAsJsonAsync(
            "/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 4.5 });

        using var request = new HttpRequestMessage(HttpMethod.Delete, "/reviews/1")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(permissions: ["reviews:administer"])) }
        };
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/reviews/1")).StatusCode);
    }

    [Fact]
    public async Task Delete_missing_review_returns_404()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/reviews/999")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(permissions: ["reviews:administer"])) }
        };
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_review_with_the_administer_permission_changes_its_content()
    {
        await _client.PostAsJsonAsync(
            "/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", title = "Old title", score = 4.5 });

        using var request = new HttpRequestMessage(HttpMethod.Put, "/reviews/1")
        {
            Content = JsonContent.Create(new { title = "Moderated title", text = "Redacted.", score = 1 }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(permissions: ["reviews:administer"])) }
        };
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var review = await _client.GetFromJsonAsync<ReviewResponse>("/reviews/1");
        Assert.NotNull(review);
        Assert.Equal("Moderated title", review.Title);
        Assert.Equal("Redacted.", review.Text);
        Assert.Equal(1m, review.Score);
    }

    [Fact]
    public async Task Update_review_without_the_administer_permission_returns_403()
    {
        await _client.PostAsJsonAsync(
            "/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 4.5 });

        var response = await _client.PutAsJsonAsync("/reviews/1", new { title = "Hijacked title" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_missing_review_returns_404()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/reviews/999")
        {
            Content = JsonContent.Create(new { title = "Doesn't matter" }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(permissions: ["reviews:administer"])) }
        };
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_review_round_trips_what_was_created()
    {
        await _client.PostAsJsonAsync("/review-users", new { id = 1, name = "Alice" });

        // ReviewUserId comes from the token, not the request body - a caller can only ever
        // create a review under the identity their own token carries.
        using var request = new HttpRequestMessage(HttpMethod.Post, "/reviews")
        {
            Content = JsonContent.Create(new
            {
                id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", title = "Great TV", score = 4.5
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(reviewUserId: 1)) }
        };
        await _client.SendAsync(request);

        var review = await _client.GetFromJsonAsync<ReviewResponse>("/reviews/1");

        Assert.NotNull(review);
        Assert.Equal("Great TV", review.Title);
        Assert.Equal(4.5m, review.Score);
        Assert.Equal(1, review.ReviewUserId);
    }

    [Fact]
    public async Task Get_missing_review_returns_404()
    {
        var response = await _client.GetAsync("/reviews/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_reviews_by_product_returns_only_reviews_for_that_product()
    {
        await _client.PostAsJsonAsync("/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 4.5 });
        await _client.PostAsJsonAsync("/reviews", new { id = 2, productId = 2, type = "Verified", creationDate = "2026-09-01", score = 3 });

        var reviews = await _client.GetFromJsonAsync<List<ReviewResponse>>("/reviews?productId=1");

        Assert.NotNull(reviews);
        var review = Assert.Single(reviews);
        Assert.Equal(1, review.Id);
    }

    [Fact]
    public async Task Average_scores_returns_the_mean_score_per_product_and_skips_unscored_products()
    {
        await _client.PostAsJsonAsync("/reviews", new { id = 1, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 4 });
        await _client.PostAsJsonAsync("/reviews", new { id = 2, productId = 1, type = "Verified", creationDate = "2026-09-01", score = 2 });
        await _client.PostAsJsonAsync("/reviews", new { id = 3, productId = 2, type = "Verified", creationDate = "2026-09-01" });

        var response = await _client.PostAsJsonAsync("/reviews/average-scores", new { productIds = new[] { 1, 2, 3 } });
        var averages = await response.Content.ReadFromJsonAsync<Dictionary<int, double>>();

        Assert.NotNull(averages);
        Assert.Equal(3, averages[1]);
        Assert.False(averages.ContainsKey(2));
        Assert.False(averages.ContainsKey(3));
    }

    private sealed record ReviewResponse(
        int Id, int ProductId, string Type, DateOnly CreationDate, string? Title, string? Text, decimal? Score, int? ReviewUserId);
}
