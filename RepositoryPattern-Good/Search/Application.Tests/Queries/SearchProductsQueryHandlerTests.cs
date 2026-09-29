using WebShop.Search.Application.Queries;
using WebShop.Search.Application.Tests.Fakes;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Models;

namespace WebShop.Search.Application.Tests.Queries;

public class SearchProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_embeds_the_query_text_and_maps_matches_to_dtos()
    {
        var repository = new FakeProductEmbeddingRepository
        {
            SimilarProductsToReturn = [new SimilarProduct(new ProductId(1), 0.9), new SimilarProduct(new ProductId(2), 0.5)]
        };
        var embeddingClient = new FakeEmbeddingClient();
        var handler = new SearchProductsQueryHandler(embeddingClient, repository);

        var results = await handler.Handle(new SearchProductsQuery("smart tv"), default);

        Assert.Equal("smart tv", embeddingClient.LastText);
        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].ProductId);
        Assert.Equal(0.9, results[0].Score);
    }
}
