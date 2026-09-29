using WebShop.Reviews.Application.Commands;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Api.Endpoints;

public static class ReviewUserEndpoints
{
    public static IEndpointRouteBuilder MapReviewUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Requires a valid token but not a specific claim - the caller has no ReviewUserId yet
        // (that's what this call is creating), so all authorization can check here is that the
        // request came from a party holding a token the Web BFF issued.
        app.MapPost("/review-users", async (ReviewUserRequest request, CreateReviewUserCommandHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.Handle(new CreateReviewUserCommand(new ReviewUserId(request.Id), request.Name, request.Email), cancellationToken);
            return Results.Created($"/review-users/{request.Id}", null);
        }).RequireAuthorization();

        return app;
    }

    private sealed record ReviewUserRequest(int Id, string Name, string? Email = null);
}
