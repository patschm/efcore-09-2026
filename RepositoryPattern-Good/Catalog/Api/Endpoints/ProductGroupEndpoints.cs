using WebShop.Catalog.Application.Commands;
using WebShop.Catalog.Application.Queries;
using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Api.Endpoints;

public static class ProductGroupEndpoints
{
    public static RouteGroupBuilder MapProductGroupEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (int? parentId, GetProductGroupsQueryHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.Handle(new GetProductGroupsQuery(parentId.HasValue ? new ProductGroupId(parentId.Value) : null), cancellationToken)));

        group.MapGet("/{id:int}", async (int id, GetProductGroupByIdQueryHandler handler, CancellationToken cancellationToken) =>
        {
            var productGroup = await handler.Handle(new GetProductGroupByIdQuery(new ProductGroupId(id)), cancellationToken);
            return productGroup is not null ? Results.Ok(productGroup) : Results.NotFound();
        });

        group.MapPost("/", async (ProductGroupRequest request, CreateProductGroupCommandHandler handler, CancellationToken cancellationToken) =>
        {
            await handler.Handle(
                new CreateProductGroupCommand(
                    new ProductGroupId(request.Id), request.Name,
                    request.ParentId.HasValue ? new ProductGroupId(request.ParentId.Value) : null, request.ImageUrl),
                cancellationToken);
            return Results.Created($"/product-groups/{request.Id}", null);
        });

        group.MapPost("/{groupId:int}/specifications", async (
            int groupId, SpecificationRequest request, DefineSpecificationCommandHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(
                new DefineSpecificationCommand(
                    new ProductGroupId(groupId), new SpecificationDefinitionId(request.Id), request.Key, request.Name,
                    request.Type, request.Unit, request.Multiple, request.Explanation),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/product-groups/{groupId}/specifications/{result.Value.Value}", null)
                : Results.BadRequest(new { errors = result.Errors });
        });

        return group;
    }

    private sealed record ProductGroupRequest(int Id, string Name, int? ParentId = null, string? ImageUrl = null);

    private sealed record SpecificationRequest(
        int Id, string Key, string Name, string? Type = null, string? Unit = null, bool Multiple = false, string? Explanation = null);
}
