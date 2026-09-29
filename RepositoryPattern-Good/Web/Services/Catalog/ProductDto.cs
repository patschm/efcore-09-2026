namespace WebShop.Web.Services.Catalog;

public sealed record ProductDto(
    int Id,
    string Name,
    int BrandId,
    string BrandName,
    int? ProductGroupId,
    string? ImageUrl,
    IReadOnlyList<ProductSpecificationDto> Specifications);

public sealed record ProductSpecificationDto(
    int SpecificationDefinitionId,
    string Name,
    string? Type,
    string? Unit,
    bool Multiple,
    string? Explanation,
    IReadOnlyList<string> Values);
