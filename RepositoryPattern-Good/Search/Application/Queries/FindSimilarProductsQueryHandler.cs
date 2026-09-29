using WebShop.Search.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Search.Application.Queries;

public sealed class FindSimilarProductsQueryHandler(IProductEmbeddingRepository repository)
    : IQueryHandler<FindSimilarProductsQuery, IReadOnlyList<SearchResultDto>>
{
    public async Task<IReadOnlyList<SearchResultDto>> Handle(FindSimilarProductsQuery query, CancellationToken cancellationToken)
    {
        var source = await repository.GetByProductId(query.ProductId, cancellationToken);
        if (source is null)
            return [];

        var matches = await repository.FindSimilar(source.Vector, query.TopN, query.ProductId, cancellationToken);
        return matches.Select(m => new SearchResultDto(m.ProductId.Value, m.Score)).ToList();
    }
}
