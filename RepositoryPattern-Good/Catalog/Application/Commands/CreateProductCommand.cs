using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Commands;

public sealed record CreateProductCommand(
    ProductId Id, string Name, BrandId BrandId, ProductGroupId? ProductGroupId = null, string? ImageUrl = null);
