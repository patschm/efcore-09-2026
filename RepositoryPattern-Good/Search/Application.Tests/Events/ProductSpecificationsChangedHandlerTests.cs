using WebShop.Catalog.Contracts;
using WebShop.Search.Application.Commands;
using WebShop.Search.Application.Events;
using WebShop.Search.Application.Tests.Fakes;

namespace WebShop.Search.Application.Tests.Events;

public class ProductSpecificationsChangedHandlerTests
{
    [Fact]
    public async Task Handle_builds_embedding_content_from_product_group_name_and_specifications()
    {
        var repository = new FakeProductEmbeddingRepository();
        var embeddingClient = new FakeEmbeddingClient();
        var upsertHandler = new UpsertProductEmbeddingCommandHandler(embeddingClient, repository);
        var handler = new ProductSpecificationsChangedHandler(upsertHandler);

        var integrationEvent = new ProductSpecificationsChangedIntegrationEvent(
            1,
            "Televisions",
            [new SpecificationSnapshot("screen_size", "Screen size", "55"), new SpecificationSnapshot("smart_tv", "Smart TV", "True")],
            DateTime.UtcNow);

        await handler.Handle(integrationEvent, default);

        Assert.Equal("Televisions. Screen size: 55; Smart TV: True", embeddingClient.LastText);
    }

    [Fact]
    public async Task Handle_with_no_specifications_still_carries_the_product_group_name()
    {
        var repository = new FakeProductEmbeddingRepository();
        var embeddingClient = new FakeEmbeddingClient();
        var upsertHandler = new UpsertProductEmbeddingCommandHandler(embeddingClient, repository);
        var handler = new ProductSpecificationsChangedHandler(upsertHandler);

        var integrationEvent = new ProductSpecificationsChangedIntegrationEvent(1, "Cameras", [], DateTime.UtcNow);

        await handler.Handle(integrationEvent, default);

        Assert.Equal("Cameras. ", embeddingClient.LastText);
    }
}
