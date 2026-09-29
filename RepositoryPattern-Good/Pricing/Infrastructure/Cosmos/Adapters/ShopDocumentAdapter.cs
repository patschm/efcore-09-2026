using WebShop.Pricing.Domain.Aggregates;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Infrastructure.Cosmos.Documents;
using WebShop.SharedKernel;

namespace WebShop.Pricing.Infrastructure.Cosmos.Adapters;

public static class ShopDocumentAdapter
{
    public static ShopDocument ToDocument(Shop shop) => new()
    {
        Id = ShopDocument.BuildId(shop.Id.Value),
        ShopId = shop.Id.Value,
        Name = shop.Name,
        Url = shop.Url.Value,
        Logo = shop.Logo?.Value,
        Rating = shop.Rating
    };

    public static Shop ToDomain(ShopDocument document) => Shop.Create(
        new ShopId(document.ShopId),
        document.Name,
        new Url(document.Url),
        Url.TryCreate(document.Logo),
        document.Rating);
}
