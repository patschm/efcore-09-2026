using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Catalog.Domain.Repositories;

namespace WebShop.Catalog.Application.Queries;

public sealed class GetProductsByProductGroupQueryHandler(IProductRepository productRepository, IBrandRepository brandRepository)
    : IQueryHandler<GetProductsByProductGroupQuery, PagedResult<ProductSummaryDto>>
{
    public async Task<PagedResult<ProductSummaryDto>> Handle(GetProductsByProductGroupQuery query, CancellationToken cancellationToken)
    {
        var totalCount = await productRepository.CountByProductGroupId(query.ProductGroupId, cancellationToken);
        var products = await productRepository.GetByProductGroupId(query.ProductGroupId, query.Page, query.PageSize, cancellationToken);

        // One batch lookup instead of a GetById per product - keeps this a single extra round
        // trip regardless of how many products the page has.
        var brandIds = products.Select(p => p.BrandId).Distinct().ToList();
        var brands = await brandRepository.GetByIds(brandIds, cancellationToken);
        var brandNamesById = brands.ToDictionary(b => b.Id, b => b.Name);

        var items = products
            .Select(p => new ProductSummaryDto(
                p.Id.Value, p.Name, p.BrandId.Value, brandNamesById.GetValueOrDefault(p.BrandId, "Unknown"), p.ImageUrl?.Value))
            .ToList();

        return new PagedResult<ProductSummaryDto>(items, totalCount, query.Page, query.PageSize);
    }
}
