using WebShop.Pricing.Domain.Identifiers;

namespace WebShop.Pricing.Application.Commands;

public sealed record CreateShopCommand(ShopId Id, string Name, string Url, string? Logo = null, double Rating = 0);
