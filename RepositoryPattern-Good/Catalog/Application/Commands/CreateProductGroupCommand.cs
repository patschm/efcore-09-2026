using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Commands;

public sealed record CreateProductGroupCommand(
    ProductGroupId Id, string Name, ProductGroupId? ParentId = null, string? ImageUrl = null);
