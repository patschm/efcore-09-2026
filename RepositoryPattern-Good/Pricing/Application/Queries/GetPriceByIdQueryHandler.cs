using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Pricing.Application.Queries;

public sealed class GetPriceByIdQueryHandler(IPriceRepository priceRepository, IShopRepository shopRepository)
    : IQueryHandler<GetPriceByIdQuery, PriceDto?>
{
    public async Task<PriceDto?> Handle(GetPriceByIdQuery query, CancellationToken cancellationToken)
    {
        var price = await priceRepository.GetById(query.Id, cancellationToken);
        if (price is null)
            return null;

        var shop = await shopRepository.GetById(price.ShopId, cancellationToken);

        return new PriceDto(
            price.Id.Value,
            price.ProductId.Value,
            price.ShopId.Value,
            shop?.Name ?? "Unknown",
            shop?.Rating ?? 0,
            price.ShopPrice.Amount,
            price.ShopPrice.Currency,
            price.ShippingPrice.Amount,
            price.ShippingPrice.Currency,
            price.TotalPrice.Amount,
            price.TotalPrice.Currency,
            price.InStock);
    }
}
