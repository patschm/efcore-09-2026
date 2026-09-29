namespace WebShop.Catalog.Application.Queries;

// Plain primitives, not the domain's strongly-typed ids - this is the boundary where the
// application hands data to a caller (API/UI) that has no reason to know about those wrapper
// types.
public sealed record ProductDto(
    int Id,
    string Name,
    int BrandId,
    string BrandName,
    int? ProductGroupId,
    string? ImageUrl,
    IReadOnlyList<ProductSpecificationDto> Specifications);

// One entry per SpecificationDefinition, not per ProductSpecificationValue row - a "Multiple"
// specification (e.g. a list of supported colors) is stored as several rows sharing the same
// SpecificationDefinitionId, and the caller wants those presented together rather than as
// separate specification lines.
public sealed record ProductSpecificationDto(
    int SpecificationDefinitionId,
    string Name,
    string? Type,
    string? Unit,
    bool Multiple,
    string? Explanation,
    IReadOnlyList<string> Values);
