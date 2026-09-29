using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.ValueObjects;

namespace WebShop.Catalog.Domain.Aggregates;

public class ProductSpecificationValue
{
    public ProductSpecificationValueId Id { get; private set; }
    public ProductId ProductId { get; private set; }
    public SpecificationDefinitionId SpecificationDefinitionId { get; private set; }
    public SpecificationValue Value { get; private set; } = null!;

    private ProductSpecificationValue()
    {
    }

    // Created only through Product.SetSpecificationValue - a spec value has no meaning
    // detached from the product it describes. Value already carries its own "exactly one
    // of three" guarantee, so there's nothing left for this class to validate.
    internal ProductSpecificationValue(
        ProductSpecificationValueId id, ProductId productId, SpecificationDefinitionId specificationDefinitionId, SpecificationValue value)
    {
        Id = id;
        ProductId = productId;
        SpecificationDefinitionId = specificationDefinitionId;
        Value = value;
    }

    internal void UpdateValue(SpecificationValue value) => Value = value;
}
