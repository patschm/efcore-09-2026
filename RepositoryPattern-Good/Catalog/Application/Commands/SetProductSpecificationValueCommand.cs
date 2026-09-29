using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Commands;

public sealed record SetProductSpecificationValueCommand(
    ProductId ProductId,
    ProductSpecificationValueId Id,
    SpecificationDefinitionId SpecificationDefinitionId,
    decimal? Number = null,
    string? Text = null,
    bool? Flag = null);
