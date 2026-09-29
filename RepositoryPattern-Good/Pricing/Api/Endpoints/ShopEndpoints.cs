using WebShop.Pricing.Application.Commands;
using WebShop.Pricing.Domain.Identifiers;

namespace WebShop.Pricing.Api.Endpoints;

public static class ShopEndpoints
{
    public static IEndpointRouteBuilder MapShopEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/shops", async (ShopRequest request, CreateShopCommandHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.Handle(
                new CreateShopCommand(new ShopId(request.Id), request.Name, request.Url, request.Logo, request.Rating),
                cancellationToken);
            return Results.Created($"/shops/{request.Id}", null);
        });

        return app;
    }

    private sealed record ShopRequest(int Id, string Name, string Url, string? Logo = null, double Rating = 0);
}
