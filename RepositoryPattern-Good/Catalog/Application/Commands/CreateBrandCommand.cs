using WebShop.Catalog.Domain.Identifiers;

namespace WebShop.Catalog.Application.Commands;

public sealed record CreateBrandCommand(BrandId Id, string Name, string? Website = null, string? Logo = null);
