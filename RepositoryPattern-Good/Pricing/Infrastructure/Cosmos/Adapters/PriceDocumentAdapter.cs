using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.ValueObjects;
using WebShop.Pricing.Infrastructure.Cosmos.Documents;

namespace WebShop.Pricing.Infrastructure.Cosmos.Adapters;

public static class PriceDocumentAdapter
{
    public static PriceDocument ToDocument(Price price, Shop? shop) => new()
    {
        Id = PriceDocument.BuildId(price.ShopId.Value),
        ProductId = price.ProductId.Value,
        PriceId = price.Id.Value,
        ShopId = price.ShopId.Value,
        ShopName = shop?.Name,
        ShopLogo = shop?.Logo?.Value,
        ShopRating = shop?.Rating,
        ShopPrice = new MoneyDocument { Amount = price.ShopPrice.Amount, Currency = price.ShopPrice.Currency },
        ShippingPrice = new MoneyDocument { Amount = price.ShippingPrice.Amount, Currency = price.ShippingPrice.Currency },
        InStock = price.InStock
    };

    public static Price ToDomain(PriceDocument document) => Price.Create(
        new PriceId(document.PriceId),
        new ProductId(document.ProductId),
        new ShopId(document.ShopId),
        new Money(document.ShopPrice.Amount, document.ShopPrice.Currency),
        new Money(document.ShippingPrice.Amount, document.ShippingPrice.Currency),
        document.InStock);
}
