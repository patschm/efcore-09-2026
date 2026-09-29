using WebShop.Pricing.Application.Commands;
using WebShop.Pricing.Application.Queries;
using WebShop.Pricing.Domain.Identifiers;

namespace WebShop.Pricing.Api.Endpoints;

public static class PriceEndpoints
{
    public static RouteGroupBuilder MapPriceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (int productId, GetPricesByProductIdQueryHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.Handle(new GetPricesByProductIdQuery(new ProductId(productId)), cancellationToken)));

        group.MapPost("/", async (PriceRequest request, CreatePriceCommandHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.Handle(
                new CreatePriceCommand(
                    new PriceId(request.Id), new ProductId(request.ProductId), new ShopId(request.ShopId),
                    request.ShopPriceAmount, request.ShopPriceCurrency, request.ShippingPriceAmount, request.ShippingPriceCurrency,
                    request.InStock),
                cancellationToken);
            return Results.Created($"/prices/{request.Id}", null);
        });

        group.MapGet("/{id:int}", async (int id, GetPriceByIdQueryHandler handler, CancellationToken cancellationToken) =>
        {
            var price = await handler.Handle(new GetPriceByIdQuery(new PriceId(id)), cancellationToken);
            return price is not null ? Results.Ok(price) : Results.NotFound();
        });

        // POST, not GET - same reason as Reviews' /reviews/average-scores: an id list has no
        // practical upper bound for a query string. A product with no prices at all is simply
        // absent from the response dictionary.
        group.MapPost("/lowest", async (LowestPricesRequest request, GetLowestPricesByProductIdsQueryHandler handler, CancellationToken cancellationToken) =>
        {
            var ids = request.ProductIds.Select(id => new ProductId(id)).ToList();
            return Results.Ok(await handler.Handle(new GetLowestPricesByProductIdsQuery(ids), cancellationToken));
        });

        return group;
    }

    private sealed record PriceRequest(
        int Id, int ProductId, int ShopId,
        double ShopPriceAmount, string ShopPriceCurrency, double ShippingPriceAmount, string ShippingPriceCurrency, int InStock);

    private sealed record LowestPricesRequest(IReadOnlyList<int> ProductIds);
}
