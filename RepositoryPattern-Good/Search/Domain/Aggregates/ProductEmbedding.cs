using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Domain.Aggregates;

// Generated/derived data produced by an embedding pipeline for similarity search - not
// business-authored, so it carries no invariants beyond "one embedding per product". Deliberately
// decoupled from Catalog: this holds only a ProductId, never a reference to Catalog's Product,
// since search/similarity has its own write lifecycle independent of business edits to a product.
//
// ContentHash (not a model name) is what this actually tracks: there has only ever been one
// embedding model, so "which model produced this" carries no information - but "has the source
// content changed since we last embedded it" does, and is what lets a caller skip a pointless
// re-embedding call.
public class ProductEmbedding
{
    public ProductId ProductId { get; private set; }
    public string Vector { get; private set; } = null!;
    public byte[] ContentHash { get; private set; } = null!;
    public DateTime GeneratedAt { get; private set; }

    private ProductEmbedding()
    {
    }

    public static ProductEmbedding Create(ProductId productId, string vector, byte[] contentHash) =>
        new()
        {
            ProductId = productId,
            Vector = vector,
            ContentHash = contentHash,
            GeneratedAt = DateTime.UtcNow
        };

    // For rebuilding from storage, not for producing a new embedding - GeneratedAt is the
    // repository's own raw-SQL read (see ProductEmbeddingRepository), not "now". Needed only
    // because reads here bypass EF's normal materialization (a native vector column can't be
    // queried through LINQ), so there's no automatic reconstruction to rely on the way a
    // regular EF-mapped aggregate gets for free.
    public static ProductEmbedding Reconstitute(ProductId productId, string vector, byte[] contentHash, DateTime generatedAt) =>
        new()
        {
            ProductId = productId,
            Vector = vector,
            ContentHash = contentHash,
            GeneratedAt = generatedAt
        };

    public void Regenerate(string vector, byte[] contentHash)
    {
        Vector = vector;
        ContentHash = contentHash;
        GeneratedAt = DateTime.UtcNow;
    }

    public bool ContentUnchanged(byte[] contentHash) => ContentHash.AsSpan().SequenceEqual(contentHash);
}
