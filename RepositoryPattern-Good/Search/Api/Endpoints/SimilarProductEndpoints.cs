using WebShop.Search.Application.Queries;
using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Api.Endpoints;

public static class SimilarProductEndpoints
{
    public static IEndpointRouteBuilder MapSimilarProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/products/{id:int}/similar", async (int id, int? topN, FindSimilarProductsQueryHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.Handle(new FindSimilarProductsQuery(new ProductId(id), topN ?? 10), cancellationToken)));

        return app;
    }
}
