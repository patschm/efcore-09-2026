using WebShop.Search.Application.Queries;
using WebShop.Search.Application.Tests.Fakes;
using WebShop.Search.Domain.Aggregates;
using WebShop.Search.Domain.Identifiers;
using WebShop.Search.Domain.Models;

namespace WebShop.Search.Application.Tests.Queries;

public class FindSimilarProductsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_empty_when_source_product_has_no_embedding()
    {
        var handler = new FindSimilarProductsQueryHandler(new FakeProductEmbeddingRepository());

        var results = await handler.Handle(new FindSimilarProductsQuery(new ProductId(999)), default);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Handle_finds_similar_products_using_the_source_products_own_vector()
    {
        var repository = new FakeProductEmbeddingRepository
        {
            SimilarProductsToReturn = [new SimilarProduct(new ProductId(2), 0.8)]
        };
        repository.Seed(ProductEmbedding.Create(new ProductId(1), "[0.1,0.2]", [1, 2, 3]));
        var handler = new FindSimilarProductsQueryHandler(repository);

        var results = await handler.Handle(new FindSimilarProductsQuery(new ProductId(1)), default);

        var result = Assert.Single(results);
        Assert.Equal(2, result.ProductId);
        Assert.Equal(0.8, result.Score);
    }
}
