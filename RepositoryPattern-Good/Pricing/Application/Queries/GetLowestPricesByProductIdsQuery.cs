using WebShop.Pricing.Domain.Identifiers;

namespace WebShop.Pricing.Application.Queries;

public sealed record GetLowestPricesByProductIdsQuery(IReadOnlyList<ProductId> ProductIds);
