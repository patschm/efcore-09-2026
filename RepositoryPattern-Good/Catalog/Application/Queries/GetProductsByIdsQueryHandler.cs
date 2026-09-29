using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Catalog.Domain.Repositories;

namespace WebShop.Catalog.Application.Queries;

public sealed class GetProductsByIdsQueryHandler(IProductRepository productRepository, IBrandRepository brandRepository)
    : IQueryHandler<GetProductsByIdsQuery, IReadOnlyList<ProductSummaryDto>>
{
    public async Task<IReadOnlyList<ProductSummaryDto>> Handle(GetProductsByIdsQuery query, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetByIds(query.ProductIds, cancellationToken);

        var brandIds = products.Select(p => p.BrandId).Distinct().ToList();
        var brands = await brandRepository.GetByIds(brandIds, cancellationToken);
        var brandNamesById = brands.ToDictionary(b => b.Id, b => b.Name);

        return products
            .Select(p => new ProductSummaryDto(
                p.Id.Value, p.Name, p.BrandId.Value, brandNamesById.GetValueOrDefault(p.BrandId, "Unknown"), p.ImageUrl?.Value))
            .ToList();
    }
}
