using WebShop.BuildingBlocks.Application.Abstractions.Messaging;
using WebShop.Pricing.Domain.Identifiers;
using WebShop.Pricing.Domain.Repositories;

namespace WebShop.Pricing.Application.Queries;

public sealed class GetLowestPricesByProductIdsQueryHandler(IPriceRepository priceRepository)
    : IQueryHandler<GetLowestPricesByProductIdsQuery, IReadOnlyDictionary<int, LowestPriceDto>>
{
    public async Task<IReadOnlyDictionary<int, LowestPriceDto>> Handle(
        GetLowestPricesByProductIdsQuery query, CancellationToken cancellationToken)
    {
        var prices = await priceRepository.GetByProductIds(query.ProductIds, cancellationToken);

        return prices
            .GroupBy(p => p.ProductId)
            .ToDictionary(
                g => g.Key.Value,
                g =>
                {
                    var lowest = g.MinBy(p => p.ShopPrice.Amount)!;
                    return new LowestPriceDto(lowest.ShopPrice.Amount, lowest.ShopPrice.Currency);
                });
    }
}
