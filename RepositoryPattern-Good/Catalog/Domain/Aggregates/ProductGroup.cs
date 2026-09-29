using WebShop.Catalog.Domain.Identifiers;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Domain.Aggregates;

public class ProductGroup
{
    private readonly List<SpecificationDefinition> _specificationDefinitions = new();
    private string _name = null!;
    private ProductGroupId? _parentId;

    public ProductGroupId Id { get; private set; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Product group name is required.", nameof(value));

            _name = value;
        }
    }

    public ProductGroupId? ParentId
    {
        get => _parentId;
        set
        {
            if (value == Id)
                throw new ArgumentException("A product group cannot be its own parent.", nameof(value));

            _parentId = value;
        }
    }

    public Url? ImageUrl { get; set; }

    public virtual IReadOnlyCollection<SpecificationDefinition> SpecificationDefinitions => _specificationDefinitions;

    private ProductGroup()
    {
    }

    // Id is assigned before ParentId below - ParentId's setter checks against Id, so
    // the initializer order here matters (Id must land first).
    public static ProductGroup Create(ProductGroupId id, string name, ProductGroupId? parentId = null, Url? imageUrl = null) =>
        new()
        {
            Id = id,
            Name = name,
            ParentId = parentId,
            ImageUrl = imageUrl
        };

    // Stays a Result-returning factory: creates a new child entity and checks it against
    // a collection-level rule (no duplicate keys) - not a single field's own value.
    public Result<SpecificationDefinition> DefineSpecification(
        SpecificationDefinitionId id,
        string key,
        string name,
        string? type = null,
        string? unit = null,
        bool multiple = false,
        string? explanation = null)
    {
        if (_specificationDefinitions.Any(s => s.Key == key))
            return Result<SpecificationDefinition>.Failure($"A specification with key '{key}' is already defined for this product group.");

        var result = SpecificationDefinition.Create(id, Id, key, name, type, unit, multiple, explanation);
        if (result.IsFailure)
            return result;

        _specificationDefinitions.Add(result.Value);
        return result;
    }
}
