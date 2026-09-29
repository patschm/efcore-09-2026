using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Catalog.Domain.Repositories;

namespace WebShop.Catalog.Application.Queries;

public sealed class GetProductGroupByIdQueryHandler(IProductGroupRepository productGroupRepository)
    : IQueryHandler<GetProductGroupByIdQuery, ProductGroupDto?>
{
    public async Task<ProductGroupDto?> Handle(GetProductGroupByIdQuery query, CancellationToken cancellationToken)
    {
        var group = await productGroupRepository.GetById(query.Id, cancellationToken);
        return group is null ? null : new ProductGroupDto(group.Id.Value, group.Name, group.ParentId?.Value, group.ImageUrl?.Value);
    }
}
