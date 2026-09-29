using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Application.Queries;

public sealed record FindSimilarProductsQuery(ProductId ProductId, int TopN = 10);
