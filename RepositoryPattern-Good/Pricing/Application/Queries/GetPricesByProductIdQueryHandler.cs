using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Pricing.Domain.Repositories;

namespace WebShop.Pricing.Application.Queries;

public sealed class GetPricesByProductIdQueryHandler(IPriceRepository priceRepository, IShopRepository shopRepository)
    : IQueryHandler<GetPricesByProductIdQuery, IReadOnlyList<PriceDto>>
{
    public async Task<IReadOnlyList<PriceDto>> Handle(GetPricesByProductIdQuery query, CancellationToken cancellationToken)
    {
        var prices = await priceRepository.GetByProductId(query.ProductId, cancellationToken);

        var shopIds = prices.Select(p => p.ShopId).Distinct().ToList();
        var shops = await shopRepository.GetByIds(shopIds, cancellationToken);
        var shopsById = shops.ToDictionary(s => s.Id);

        return prices
            .Select(price =>
            {
                var shop = shopsById.GetValueOrDefault(price.ShopId);
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
            })
            .ToList();
    }
}
