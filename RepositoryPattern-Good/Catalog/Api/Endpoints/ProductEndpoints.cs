using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Application.Queries;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (int productGroupId, int? page, int? pageSize, GetProductsByProductGroupQueryHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.Handle(
                new GetProductsByProductGroupQuery(new ProductGroupId(productGroupId), page ?? 1, pageSize ?? 20), cancellationToken)));

        group.MapPost("/", async (ProductRequest request, CreateProductCommandHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.Handle(
                new CreateProductCommand(
                    new ProductId(request.Id), request.Name, new BrandId(request.BrandId),
                    request.ProductGroupId.HasValue ? new ProductGroupId(request.ProductGroupId.Value) : null, request.ImageUrl),
                cancellationToken);
            return Results.Created($"/products/{request.Id}", null);
        });

        group.MapPut("/{productId:int}/specification-values/{specificationDefinitionId:int}", async (
            int productId, int specificationDefinitionId, SpecificationValueRequest request,
            SetProductSpecificationValueCommandHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new SetProductSpecificationValueCommand(
                    new ProductId(productId), new ProductSpecificationValueId(request.Id), new SpecificationDefinitionId(specificationDefinitionId),
                    request.Number, request.Text, request.Flag),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(new { errors = result.Errors });
        });

        group.MapGet("/{id:int}", async (int id, GetProductByIdQueryHandler handler, CancellationToken cancellationToken) =>
        {
            var product = await handler.Handle(new GetProductByIdQuery(new ProductId(id)), cancellationToken);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        });

        // POST, not GET - same reason as Reviews' /reviews/average-scores: an id list has no
        // practical upper bound for a query string.
        group.MapPost("/by-ids", async (ByIdsRequest request, GetProductsByIdsQueryHandler handler, CancellationToken cancellationToken) =>
        {
            var ids = request.ProductIds.Select(id => new ProductId(id)).ToList();
            return Results.Ok(await handler.Handle(new GetProductsByIdsQuery(ids), cancellationToken));
        });

        return group;
    }

    private sealed record ProductRequest(int Id, string Name, int BrandId, int? ProductGroupId = null, string? ImageUrl = null);

    private sealed record SpecificationValueRequest(int Id, decimal? Number = null, string? Text = null, bool? Flag = null);

    private sealed record ByIdsRequest(IReadOnlyList<int> ProductIds);
}
