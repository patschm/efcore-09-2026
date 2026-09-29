namespace WebShop.Pricing.Application.Queries;

public sealed record PriceDto(
    int Id,
    int ProductId,
    int ShopId,
    string ShopName,
    double ShopRating,
    double ShopPriceAmount,
    string ShopPriceCurrency,
    double ShippingPriceAmount,
    string ShippingPriceCurrency,
    double TotalPriceAmount,
    string TotalPriceCurrency,
    int InStock);
