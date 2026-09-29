using WebShop.Reviews.Application.Commands;
using WebShop.Reviews.Application.Queries;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Api.Endpoints;

public static class ReviewEndpoints
{
    public static RouteGroupBuilder MapReviewEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (int productId, GetReviewsByProductIdQueryHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.Handle(new GetReviewsByProductIdQuery(new ProductId(productId)), cancellationToken)));

        // POST, not GET - the product id list has no practical upper bound (a large product
        // group easily exceeds the URL length limit a query string would need).
        group.MapPost("/average-scores", async (AverageScoresRequest request, GetAverageScoresByProductIdsQueryHandler handler, CancellationToken cancellationToken) =>
        {
            var ids = request.ProductIds.Select(id => new ProductId(id)).ToList();
            return Results.Ok(await handler.Handle(new GetAverageScoresByProductIdsQuery(ids), cancellationToken));
        });

        // ReviewUserId is deliberately not part of ReviewRequest - it comes from the caller's
        // token, not the request body, so an authenticated caller can never post a review under
        // someone else's identity by just naming a different id.
        group.MapPost("/", async (ReviewRequest request, HttpContext httpContext, CreateReviewCommandHandler handler, CancellationToken cancellationToken) =>
        {
            var reviewUserId = httpContext.User.FindFirst("reviewUserId")?.Value;

            await handler.Handle(
                new CreateReviewCommand(
                    new ReviewId(request.Id), new ProductId(request.ProductId), request.Type, request.CreationDate,
                    request.Title, request.Text, request.Score, reviewUserId is not null ? new ReviewUserId(int.Parse(reviewUserId)) : null),
                cancellationToken);
            return Results.Created($"/reviews/{request.Id}", null);
        }).RequireAuthorization("CanCreateReviews");

        group.MapGet("/{id:int}", async (int id, GetReviewByIdQueryHandler handler, CancellationToken cancellationToken) =>
        {
            var review = await handler.Handle(new GetReviewByIdQuery(new ReviewId(id)), cancellationToken);
            return review is not null ? Results.Ok(review) : Results.NotFound();
        });

        group.MapDelete("/{id:int}", async (int id, DeleteReviewCommandHandler handler, CancellationToken cancellationToken) =>
        {
            var deleted = await handler.Handle(new DeleteReviewCommand(new ReviewId(id)), cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("CanAdministerReviews");

        // Moderation, not authorship - an administrator can correct/redact any review's content,
        // but this never changes whose review it is (ReviewUserId isn't part of the request).
        group.MapPut("/{id:int}", async (int id, UpdateReviewRequest request, UpdateReviewCommandHandler handler, CancellationToken cancellationToken) =>
        {
            var updated = await handler.Handle(
                new UpdateReviewCommand(new ReviewId(id), request.Title, request.Text, request.Score), cancellationToken);
            return updated ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization("CanAdministerReviews");

        return group;
    }

    private sealed record ReviewRequest(
        int Id, int ProductId, string Type, DateOnly CreationDate,
        string? Title = null, string? Text = null, decimal? Score = null);

    private sealed record UpdateReviewRequest(string? Title = null, string? Text = null, decimal? Score = null);

    private sealed record AverageScoresRequest(IReadOnlyList<int> ProductIds);
}
