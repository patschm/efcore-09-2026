using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Domain.Tests.Aggregates;

public class ProductEmbeddingTests
{
    private static readonly byte[] HashA = [1, 2, 3];
    private static readonly byte[] HashB = [4, 5, 6];

    [Fact]
    public void Create_sets_vector_and_content_hash()
    {
        var embedding = ProductEmbedding.Create(new ProductId(1), "[0.1,0.2]", HashA);

        Assert.Equal("[0.1,0.2]", embedding.Vector);
        Assert.Equal(HashA, embedding.ContentHash);
    }

    [Fact]
    public void Regenerate_replaces_vector_and_content_hash()
    {
        var embedding = ProductEmbedding.Create(new ProductId(1), "[0.1,0.2]", HashA);

        embedding.Regenerate("[0.3,0.4]", HashB);

        Assert.Equal("[0.3,0.4]", embedding.Vector);
        Assert.Equal(HashB, embedding.ContentHash);
    }

    [Fact]
    public void ContentUnchanged_is_true_for_the_same_hash_bytes()
    {
        var embedding = ProductEmbedding.Create(new ProductId(1), "[0.1,0.2]", HashA);

        Assert.True(embedding.ContentUnchanged([1, 2, 3]));
        Assert.False(embedding.ContentUnchanged(HashB));
    }

    [Fact]
    public void Reconstitute_preserves_the_given_generated_at_instead_of_stamping_now()
    {
        var generatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var embedding = ProductEmbedding.Reconstitute(new ProductId(1), "[0.1,0.2]", HashA, generatedAt);

        Assert.Equal(generatedAt, embedding.GeneratedAt);
    }
}
