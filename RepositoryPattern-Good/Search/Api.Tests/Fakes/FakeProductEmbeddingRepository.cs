using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Models;
using WebShop.Search.Domain.Repositories;

namespace WebShop.Search.Api.Tests.Fakes;

internal sealed class FakeProductEmbeddingRepository : IProductEmbeddingRepository
{
    private readonly Dictionary<ProductId, ProductEmbedding> _embeddings = [];

    public Task<ProductEmbedding?> GetByProductId(ProductId productId, CancellationToken cancellationToken) =>
        Task.FromResult(_embeddings.GetValueOrDefault(productId));

    public Task Upsert(ProductEmbedding embedding, CancellationToken cancellationToken)
    {
        _embeddings[embedding.ProductId] = embedding;
        return Task.CompletedTask;
    }

    // No real similarity math here - this only proves the API's routing/serialization wiring
    // (see the Search smoke test and Search.Application.Tests for real ranking coverage against
    // real pgvector and controlled fakes, respectively).
    public Task<IReadOnlyList<SimilarProduct>> FindSimilar(string vector, int topN, ProductId? exclude, CancellationToken cancellationToken)
    {
        var matches = _embeddings.Values
            .Where(e => exclude is null || e.ProductId != exclude.Value)
            .Take(topN)
            .Select(e => new SimilarProduct(e.ProductId, 1.0))
            .ToList();

        return Task.FromResult<IReadOnlyList<SimilarProduct>>(matches);
    }
}
