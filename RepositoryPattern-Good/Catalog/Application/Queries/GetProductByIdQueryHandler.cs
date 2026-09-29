using System.Globalization;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.Repositories;
using WebShop.Catalog.Domain.ValueObjects;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Catalog.Application.Queries;

// Goes through IProductRepository (the write-side aggregate loader) rather than a separate
// read path straight to the DbContext - a deliberate simplification for now, since this
// project has no evidence yet of a read/write shape divergence that would justify a second
// query-side abstraction. Revisit if/when that changes.
public sealed class GetProductByIdQueryHandler(
    IProductRepository productRepository, IProductGroupRepository productGroupRepository, IBrandRepository brandRepository)
    : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetById(query.Id, cancellationToken);
        if (product is null)
            return null;

        var brand = await brandRepository.GetById(product.BrandId, cancellationToken);

        return new ProductDto(
            product.Id.Value,
            product.Name,
            product.BrandId.Value,
            brand?.Name ?? "Unknown",
            product.ProductGroupId?.Value,
            product.ImageUrl?.Value,
            await BuildSpecifications(product.ProductGroupId, product.SpecificationValues, cancellationToken));
    }

    // Specification metadata (name/type/unit/explanation) lives on the ProductGroup, not on the
    // product itself - a definition only makes sense within the taxonomy of the group that owns
    // it, so it has to be loaded separately and joined in here.
    private async Task<IReadOnlyList<ProductSpecificationDto>> BuildSpecifications(
        ProductGroupId? productGroupId, IEnumerable<Domain.Aggregates.ProductSpecificationValue> values, CancellationToken cancellationToken)
    {
        if (productGroupId is null)
            return [];

        var productGroup = await productGroupRepository.GetById(productGroupId.Value, cancellationToken);
        if (productGroup is null)
            return [];

        var definitionsById = productGroup.SpecificationDefinitions.ToDictionary(d => d.Id);

        return values
            .Where(v => definitionsById.ContainsKey(v.SpecificationDefinitionId))
            .GroupBy(v => v.SpecificationDefinitionId)
            .Select(group =>
            {
                var definition = definitionsById[group.Key];
                return new ProductSpecificationDto(
                    group.Key.Value,
                    definition.Name,
                    definition.Type,
                    definition.Unit,
                    definition.Multiple,
                    definition.Explanation,
                    group.Select(v => Describe(v.Value, definition.Unit)).ToList());
            })
            .ToList();
    }

    private static string Describe(SpecificationValue value, string? unit)
    {
        if (value.Flag.HasValue)
            return value.Flag.Value ? "Yes" : "No";
        if (value.Number.HasValue)
        {
            var number = value.Number.Value.ToString("0.########", CultureInfo.InvariantCulture);
            return unit is null ? number : $"{number} {unit}";
        }

        return value.Text ?? string.Empty;
    }
}
