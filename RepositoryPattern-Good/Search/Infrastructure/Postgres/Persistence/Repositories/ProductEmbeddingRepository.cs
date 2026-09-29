using Microsoft.EntityFrameworkCore;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Models;
using WebShop.Search.Domain.Repositories;
using WebShop.Search.Infrastructure.Postgres.Persistence.Contexts;

namespace WebShop.Search.Infrastructure.Postgres.Persistence.Repositories;

public sealed class ProductEmbeddingRepository(SearchPgContext context) : IProductEmbeddingRepository
{
    // Raw SQL, same reason as Upsert/FindSimilar below: reading Vector through normal LINQ
    // would ask Npgsql to materialize a native vector column as a plain string, which isn't
    // a supported conversion without the pgvector plugin - CAST to text sidesteps that.
    public async Task<ProductEmbedding?> GetByProductId(ProductId productId, CancellationToken cancellationToken)
    {
        var rows = await context.Database.SqlQueryRaw<EmbeddingRow>(
            """
            SELECT "ProductId", CAST("Embedding" AS text) AS "Vector", "ContentHash", "GeneratedAt"
            FROM public."ProductEmbeddingQwen3"
            WHERE "ProductId" = {0}
            """,
            productId.Value)
            .ToListAsync(cancellationToken);

        var row = rows.SingleOrDefault();
        return row is null ? null : ProductEmbedding.Reconstitute(new ProductId(row.ProductId), row.Vector, row.ContentHash, row.GeneratedAt);
    }

    public Task Upsert(ProductEmbedding embedding, CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public."ProductEmbeddingQwen3" ("ProductId", "Embedding", "ContentHash", "GeneratedAt")
            VALUES ({0}, CAST({1} AS vector(1024)), {2}, {3})
            ON CONFLICT ("ProductId") DO UPDATE SET
                "Embedding" = EXCLUDED."Embedding", "ContentHash" = EXCLUDED."ContentHash", "GeneratedAt" = EXCLUDED."GeneratedAt"
            """,
            [embedding.ProductId.Value, embedding.Vector, embedding.ContentHash, embedding.GeneratedAt],
            cancellationToken);

    // Cosine distance via pgvector's <=> operator; 1 - distance gives a 0..1 similarity score.
    // Vector is a plain string on the aggregate (see ProductEmbedding), so - like the write
    // path - this has to be raw SQL rather than LINQ. The explicit ::int cast on the exclude
    // parameter avoids Postgres rejecting an untyped null when no product is excluded.
    public async Task<IReadOnlyList<SimilarProduct>> FindSimilar(string vector, int topN, ProductId? exclude, CancellationToken cancellationToken)
    {
        object excludeParam = exclude.HasValue ? exclude.Value.Value : DBNull.Value;

        var matches = await context.Database.SqlQueryRaw<EmbeddingMatch>(
            """
            SELECT "ProductId", 1 - ("Embedding" <=> CAST({0} AS vector(1024))) AS "Score"
            FROM public."ProductEmbeddingQwen3"
            WHERE {1}::int IS NULL OR "ProductId" <> {1}::int
            ORDER BY "Embedding" <=> CAST({0} AS vector(1024))
            LIMIT {2}
            """,
            vector, excludeParam, topN)
            .ToListAsync(cancellationToken);

        return matches.Select(m => new SimilarProduct(new ProductId(m.ProductId), m.Score)).ToList();
    }

    private sealed record EmbeddingMatch(int ProductId, double Score);
    private sealed record EmbeddingRow(int ProductId, string Vector, byte[] ContentHash, DateTime GeneratedAt);
}
