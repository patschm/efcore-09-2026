using WebShop.Search.Application.Queries;

namespace WebShop.Search.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/search", async (string text, int? topN, SearchProductsQueryHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.Handle(new SearchProductsQuery(text, topN ?? 10), cancellationToken)));

        return app;
    }
}
