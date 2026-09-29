using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Ports;
using WebShop.Search.Domain.Repositories;
using WebShop.BuildingBlocks.Application.Abstractions.Messaging;

namespace WebShop.Search.Application.Queries;

public sealed class SearchProductsQueryHandler(
    IEmbeddingClient embeddingClient,
    IProductEmbeddingRepository repository) : IQueryHandler<SearchProductsQuery, IReadOnlyList<SearchResultDto>>
{
    public async Task<IReadOnlyList<SearchResultDto>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        var vector = await embeddingClient.Embed(query.Text, cancellationToken);
        var matches = await repository.FindSimilar(vector, query.TopN, exclude: null, cancellationToken);
        return matches.Select(m => new SearchResultDto(m.ProductId.Value, m.Score)).ToList();
    }
}
