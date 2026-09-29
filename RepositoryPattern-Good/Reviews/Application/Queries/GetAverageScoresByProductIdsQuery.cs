using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Queries;

public sealed record GetAverageScoresByProductIdsQuery(IReadOnlyList<ProductId> ProductIds);
