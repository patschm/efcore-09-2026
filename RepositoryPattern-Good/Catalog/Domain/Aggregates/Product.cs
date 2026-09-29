using WebShop.Catalog.Domain.Events;
using WebShop.Catalog.Domain.Identifiers;
using WebShop.Catalog.Domain.ValueObjects;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Domain.Aggregates;

public class Product : AggregateRoot
{
    private readonly List<ProductSpecificationValue> _specificationValues = new();
    private string _name = null!;

    public ProductId Id { get; private set; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Product name is required.", nameof(value));

            _name = value;
        }
    }

    // Reference other Catalog aggregate roots by id only - Brand and ProductGroup
    // are loaded independently, not through Product.
    public BrandId BrandId { get; set; }
    public ProductGroupId? ProductGroupId { get; set; }
    public Url? ImageUrl { get; set; }

    public virtual IReadOnlyCollection<ProductSpecificationValue> SpecificationValues => _specificationValues;

    private Product()
    {
    }

    public static Product Create(ProductId id, string name, BrandId brandId, ProductGroupId? productGroupId = null, Url? imageUrl = null)
    {
        var product = new Product
        {
            Id = id,
            Name = name,
            BrandId = brandId,
            ProductGroupId = productGroupId,
            ImageUrl = imageUrl
        };

        // Other contexts (e.g. Search, to generate an embedding) learn about a new product
        // through this - not through a direct reference to Product.
        product.Raise(new ProductCreated(id, DateTime.UtcNow));
        return product;
    }

    // For rebuilding from storage without re-raising ProductSpecificationValueSet - a non-EF
    // read path (e.g. a Cosmos repository) has no automatic materialization to rely on the way
    // EF's change tracker gives it for free, so it needs an explicit way in. Same reasoning as
    // ProductEmbedding.Reconstitute in the Search context.
    public static Product Reconstitute(
        ProductId id,
        string name,
        BrandId brandId,
        ProductGroupId? productGroupId,
        Url? imageUrl,
        IEnumerable<(ProductSpecificationValueId Id, SpecificationDefinitionId SpecificationDefinitionId, SpecificationValue Value)> specificationValues)
    {
        var product = new Product
        {
            Id = id,
            Name = name,
            BrandId = brandId,
            ProductGroupId = productGroupId,
            ImageUrl = imageUrl
        };

        foreach (var (specValueId, specDefId, value) in specificationValues)
            product._specificationValues.Add(new ProductSpecificationValue(specValueId, id, specDefId, value));

        return product;
    }

    public ProductSpecificationValue SetSpecificationValue(
        ProductSpecificationValueId id, SpecificationDefinitionId specificationDefinitionId, SpecificationValue value)
    {
        var existing = _specificationValues.FirstOrDefault(v => v.SpecificationDefinitionId == specificationDefinitionId);
        ProductSpecificationValue result;
        if (existing is not null)
        {
            existing.UpdateValue(value);
            result = existing;
        }
        else
        {
            result = new ProductSpecificationValue(id, Id, specificationDefinitionId, value);
            _specificationValues.Add(result);
        }

        Raise(new ProductSpecificationValueSet(Id, DateTime.UtcNow));
        return result;
    }
}
