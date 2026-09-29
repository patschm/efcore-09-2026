using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Commands;

public sealed record DefineSpecificationCommand(
    ProductGroupId ProductGroupId,
    SpecificationDefinitionId Id,
    string Key,
    string Name,
    string? Type = null,
    string? Unit = null,
    bool Multiple = false,
    string? Explanation = null);
