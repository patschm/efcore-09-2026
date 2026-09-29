using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Catalog.Domain.Repositories;

namespace WebShop.Catalog.Application.Queries;

public sealed class GetProductGroupsQueryHandler(IProductGroupRepository productGroupRepository)
    : IQueryHandler<GetProductGroupsQuery, IReadOnlyList<ProductGroupDto>>
{
    public async Task<IReadOnlyList<ProductGroupDto>> Handle(GetProductGroupsQuery query, CancellationToken cancellationToken)
    {
        var groups = await productGroupRepository.GetByParentId(query.ParentId, cancellationToken);
        return groups
            .Select(g => new ProductGroupDto(g.Id.Value, g.Name, g.ParentId?.Value, g.ImageUrl?.Value))
            .ToList();
    }
}
