using WebShop.Pricing.Domain.Identifiers;

namespace WebShop.Pricing.Application.Queries;

public sealed record GetPricesByProductIdQuery(ProductId ProductId);
