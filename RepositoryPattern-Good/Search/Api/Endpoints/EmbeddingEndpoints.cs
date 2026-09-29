using WebShop.Search.Application.Commands;
using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Api.Endpoints;

public static class EmbeddingEndpoints
{
    public static IEndpointRouteBuilder MapEmbeddingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/embeddings", async (EmbeddingRequest request, UpsertProductEmbeddingCommandHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.Handle(new UpsertProductEmbeddingCommand(new ProductId(request.ProductId), request.Content), cancellationToken);
            return Results.NoContent();
        });

        return app;
    }

    private sealed record EmbeddingRequest(int ProductId, string Content);
}
