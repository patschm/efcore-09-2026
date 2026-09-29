using Microsoft.EntityFrameworkCore;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Models;
using WebShop.Search.Domain.Repositories;
using WebShop.Search.Infrastructure.SqlServer.Persistence.Contexts;

namespace WebShop.Search.Infrastructure.SqlServer.Persistence.Repositories;

public sealed class ProductEmbeddingRepository(SearchContext context) : IProductEmbeddingRepository
{
    // Raw SQL for the same reason as Upsert/FindSimilar: a native VECTOR column doesn't
    // convert to a plain string through normal LINQ materialization.
    public async Task<ProductEmbedding?> GetByProductId(ProductId productId, CancellationToken cancellationToken)
    {
        var rows = await context.Database.SqlQueryRaw<EmbeddingRow>(
            """
            SELECT ProductId, CAST(Vector AS NVARCHAR(MAX)) AS Vector, ContentHash, GeneratedAt
            FROM ProductEmbeddings
            WHERE ProductId = {0}
            """,
            productId.Value)
            .ToListAsync(cancellationToken);

        var row = rows.SingleOrDefault();
        return row is null ? null : ProductEmbedding.Reconstitute(new ProductId(row.ProductId), row.Vector, row.ContentHash, row.GeneratedAt);
    }

    public Task Upsert(ProductEmbedding embedding, CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync(
            """
            MERGE ProductEmbeddings AS target
            USING (SELECT {0} AS ProductId) AS source
            ON target.ProductId = source.ProductId
            WHEN MATCHED THEN UPDATE SET Vector = CAST({1} AS VECTOR(1024)), ContentHash = {2}, GeneratedAt = {3}
            WHEN NOT MATCHED THEN INSERT (ProductId, Vector, ContentHash, GeneratedAt)
                VALUES ({0}, CAST({1} AS VECTOR(1024)), {2}, {3});
            """,
            [embedding.ProductId.Value, embedding.Vector, embedding.ContentHash, embedding.GeneratedAt],
            cancellationToken);

    // Mirrors the Postgres pgvector path (see the Postgres ProductEmbeddingRepository) using
    // SQL Server 2025's VECTOR_DISTANCE function - unverified against a live SQL Server
    // instance, same caveat the old vector(1024) mapping already carried in this codebase.
    public async Task<IReadOnlyList<SimilarProduct>> FindSimilar(string vector, int topN, ProductId? exclude, CancellationToken cancellationToken)
    {
        object excludeParam = exclude.HasValue ? exclude.Value.Value : DBNull.Value;

        var matches = await context.Database.SqlQueryRaw<EmbeddingMatch>(
            """
            SELECT TOP ({2}) ProductId,
                   1 - VECTOR_DISTANCE('cosine', Vector, CAST({0} AS VECTOR(1024))) AS Score
            FROM ProductEmbeddings
            WHERE {1} IS NULL OR ProductId <> {1}
            ORDER BY VECTOR_DISTANCE('cosine', Vector, CAST({0} AS VECTOR(1024)))
            """,
            vector, excludeParam, topN)
            .ToListAsync(cancellationToken);

        return matches.Select(m => new SimilarProduct(new ProductId(m.ProductId), m.Score)).ToList();
    }

    private sealed record EmbeddingMatch(int ProductId, double Score);
    private sealed record EmbeddingRow(int ProductId, string Vector, byte[] ContentHash, DateTime GeneratedAt);
}
