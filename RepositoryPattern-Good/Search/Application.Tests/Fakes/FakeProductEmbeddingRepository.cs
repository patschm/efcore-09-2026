using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Models;
using WebShop.Search.Domain.Repositories;

namespace WebShop.Search.Application.Tests.Fakes;

internal sealed class FakeProductEmbeddingRepository : IProductEmbeddingRepository
{
    private readonly Dictionary<ProductId, ProductEmbedding> _embeddings = [];

    public IReadOnlyList<SimilarProduct> SimilarProductsToReturn { get; set; } = [];
    public ProductEmbedding? LastUpserted { get; private set; }

    public Task<ProductEmbedding?> GetByProductId(ProductId productId, CancellationToken cancellationToken) =>
        Task.FromResult(_embeddings.GetValueOrDefault(productId));

    public Task Upsert(ProductEmbedding embedding, CancellationToken cancellationToken)
    {
        _embeddings[embedding.ProductId] = embedding;
        LastUpserted = embedding;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SimilarProduct>> FindSimilar(string vector, int topN, ProductId? exclude, CancellationToken cancellationToken) =>
        Task.FromResult(SimilarProductsToReturn);

    public void Seed(ProductEmbedding embedding) => _embeddings[embedding.ProductId] = embedding;
}
