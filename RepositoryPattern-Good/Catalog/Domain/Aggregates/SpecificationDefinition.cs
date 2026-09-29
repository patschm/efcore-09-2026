using WebShop.Catalog.Domain.Identifiers;
using WebShop.SharedKernel;

namespace WebShop.Catalog.Domain.Aggregates;

public class SpecificationDefinition
{
    public SpecificationDefinitionId Id { get; private set; }
    public ProductGroupId ProductGroupId { get; private set; }
    public string Key { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Unit { get; private set; }
    public string? Type { get; private set; }
    public bool Multiple { get; private set; }
    public string? Explanation { get; private set; }

    private SpecificationDefinition()
    {
    }

    private SpecificationDefinition(
        SpecificationDefinitionId id,
        ProductGroupId productGroupId,
        string key,
        string name,
        string? type,
        string? unit,
        bool multiple,
        string? explanation)
    {
        Id = id;
        ProductGroupId = productGroupId;
        Key = key;
        Name = name;
        Type = type;
        Unit = unit;
        Multiple = multiple;
        Explanation = explanation;
    }

    // Created only through ProductGroup.DefineSpecification - a specification's key/type/unit
    // only make sense within the taxonomy of the product group that owns it.
    internal static Result<SpecificationDefinition> Create(
        SpecificationDefinitionId id,
        ProductGroupId productGroupId,
        string key,
        string name,
        string? type,
        string? unit,
        bool multiple,
        string? explanation)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Result<SpecificationDefinition>.Failure("Specification key is required.");
        if (string.IsNullOrWhiteSpace(name))
            return Result<SpecificationDefinition>.Failure("Specification name is required.");

        return Result<SpecificationDefinition>.Success(
            new SpecificationDefinition(id, productGroupId, key, name, type, unit, multiple, explanation));
    }
}
