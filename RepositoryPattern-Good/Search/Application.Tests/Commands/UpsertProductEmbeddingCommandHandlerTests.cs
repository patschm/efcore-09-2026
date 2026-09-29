using System.Security.Cryptography;
using System.Text;
using WebShop.Search.Application.Commands;
using WebShop.Search.Application.Tests.Fakes;
using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Identifiers;

namespace WebShop.Search.Application.Tests.Commands;

public class UpsertProductEmbeddingCommandHandlerTests
{
    [Fact]
    public async Task Handle_creates_a_new_embedding_when_none_exists()
    {
        var repository = new FakeProductEmbeddingRepository();
        var embeddingClient = new FakeEmbeddingClient { VectorToReturn = "[0.1,0.2]" };
        var handler = new UpsertProductEmbeddingCommandHandler(embeddingClient, repository);

        await handler.Handle(new UpsertProductEmbeddingCommand(new ProductId(1), "smart tv 55 inch"), default);

        Assert.Equal("smart tv 55 inch", embeddingClient.LastText);
        Assert.NotNull(repository.LastUpserted);
        Assert.Equal("[0.1,0.2]", repository.LastUpserted.Vector);
        Assert.Equal(Sha256("smart tv 55 inch"), repository.LastUpserted.ContentHash);
    }

    [Fact]
    public async Task Handle_regenerates_the_existing_embedding_when_content_changed()
    {
        var repository = new FakeProductEmbeddingRepository();
        repository.Seed(ProductEmbedding.Create(new ProductId(1), "[old]", Sha256("old content")));
        var embeddingClient = new FakeEmbeddingClient { VectorToReturn = "[new]" };
        var handler = new UpsertProductEmbeddingCommandHandler(embeddingClient, repository);

        await handler.Handle(new UpsertProductEmbeddingCommand(new ProductId(1), "updated content"), default);

        Assert.Equal(1, embeddingClient.CallCount);
        Assert.Equal("[new]", repository.LastUpserted!.Vector);
        Assert.Equal(Sha256("updated content"), repository.LastUpserted.ContentHash);
    }

    [Fact]
    public async Task Handle_skips_regeneration_when_content_is_unchanged()
    {
        var repository = new FakeProductEmbeddingRepository();
        repository.Seed(ProductEmbedding.Create(new ProductId(1), "[existing]", Sha256("same content")));
        var embeddingClient = new FakeEmbeddingClient { VectorToReturn = "[should-not-be-used]" };
        var handler = new UpsertProductEmbeddingCommandHandler(embeddingClient, repository);

        await handler.Handle(new UpsertProductEmbeddingCommand(new ProductId(1), "same content"), default);

        Assert.Equal(0, embeddingClient.CallCount);
        Assert.Null(repository.LastUpserted);
    }

    private static byte[] Sha256(string content) => SHA256.HashData(Encoding.UTF8.GetBytes(content));
}
