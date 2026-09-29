using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Api.Endpoints;

public static class BrandEndpoints
{
    public static IEndpointRouteBuilder MapBrandEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/brands", async (BrandRequest request, CreateBrandCommandHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.Handle(new CreateBrandCommand(new BrandId(request.Id), request.Name, request.Website, request.Logo), cancellationToken);
            return Results.Created($"/brands/{request.Id}", null);
        });

        return app;
    }

    private sealed record BrandRequest(int Id, string Name, string? Website = null, string? Logo = null);
}
