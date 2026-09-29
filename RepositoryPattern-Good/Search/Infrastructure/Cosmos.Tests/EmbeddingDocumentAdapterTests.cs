using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Infrastructure.Cosmos.Adapters;
using WebShop.Search.Infrastructure.Cosmos.Documents;

namespace WebShop.Search.Infrastructure.Cosmos.Tests;

public class EmbeddingDocumentAdapterTests
{
    [Fact]
    public void Round_trips_the_vector_content_hash_and_generated_timestamp()
    {
        var generatedAt = new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);
        var embedding = ProductEmbedding.Reconstitute(new ProductId(112), "[0.1,0.2,0.3]", [1, 2, 3], generatedAt);

        var loaded = EmbeddingDocumentAdapter.ToDomain(EmbeddingDocumentAdapter.ToDocument(embedding));

        Assert.Equal(112, loaded.ProductId.Value);
        Assert.Equal("[0.1,0.2,0.3]", loaded.Vector);
        Assert.Equal<byte[]>([1, 2, 3], loaded.ContentHash);
        Assert.Equal(generatedAt, loaded.GeneratedAt);
    }

    [Fact]
    public void ToDocument_parses_the_pgvector_string_into_a_numeric_array_for_the_vector_index()
    {
        var embedding = ProductEmbedding.Reconstitute(new ProductId(1), "[1,2,3]", [0], DateTime.UtcNow);

        var document = EmbeddingDocumentAdapter.ToDocument(embedding);

        Assert.Equal([1.0, 2.0, 3.0], document.Vector);
    }

    [Fact]
    public void BuildId_is_scoped_to_the_product()
    {
        Assert.Equal("embedding|112", EmbeddingDocument.BuildId(112));
    }
}
