using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Models;

namespace WebShop.Search.Domain.Repositories;

public interface IProductEmbeddingRepository
{
    Task<ProductEmbedding?> GetByProductId(ProductId productId, CancellationToken cancellationToken);

    // Writes immediately via raw SQL rather than going through IUnitOfWork - the vector column
    // can't round-trip through the normal SaveChanges pipeline (confirmed the same way the old
    // Catalog embedding column was: SaveChanges sends the value as varchar, which Postgres/SQL
    // Server both reject for a vector column), so there's nothing to defer.
    Task Upsert(ProductEmbedding embedding, CancellationToken cancellationToken);

    // exclude is null for a text search, and set to the source product for "similar products".
    Task<IReadOnlyList<SimilarProduct>> FindSimilar(string vector, int topN, ProductId? exclude, CancellationToken cancellationToken);
}
