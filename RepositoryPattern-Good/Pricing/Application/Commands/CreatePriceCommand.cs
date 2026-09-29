using WebShop.Pricing.Domain.Identifiers;

namespace WebShop.Pricing.Application.Commands;

public sealed record CreatePriceCommand(
    PriceId Id,
    ProductId ProductId,
    ShopId ShopId,
    double ShopPriceAmount,
    string ShopPriceCurrency,
    double ShippingPriceAmount,
    string ShippingPriceCurrency,
    int InStock);
