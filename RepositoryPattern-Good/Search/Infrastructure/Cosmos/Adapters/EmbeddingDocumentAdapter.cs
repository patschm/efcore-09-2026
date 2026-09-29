using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Infrastructure.Cosmos.Documents;

namespace WebShop.Search.Infrastructure.Cosmos.Adapters;

public static class EmbeddingDocumentAdapter
{
    public static EmbeddingDocument ToDocument(ProductEmbedding embedding) => new()
    {
        Id = EmbeddingDocument.BuildId(embedding.ProductId.Value),
        ProductId = embedding.ProductId.Value,
        Vector = EmbeddingVectorFormat.Parse(embedding.Vector),
        ContentHash = embedding.ContentHash,
        GeneratedAt = embedding.GeneratedAt
    };

    public static ProductEmbedding ToDomain(EmbeddingDocument document) => ProductEmbedding.Reconstitute(
        new ProductId(document.ProductId),
        EmbeddingVectorFormat.Format(document.Vector),
        document.ContentHash,
        document.GeneratedAt);
}
