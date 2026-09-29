using WebShop.Reviews.Application.Queries;
using WebShop.Reviews.Application.Tests.Fakes;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;

namespace WebShop.Reviews.Application.Tests.Queries;

public class GetAverageScoresByProductIdsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_the_mean_score_per_product_and_skips_unscored_products()
    {
        var repository = new FakeReviewRepository();
        var today = DateOnly.FromDateTime(DateTime.Today);
        repository.Add(Review.Create(new ReviewId(1), new ProductId(1), "Written", today, score: 4));
        repository.Add(Review.Create(new ReviewId(2), new ProductId(1), "Written", today, score: 2));
        repository.Add(Review.Create(new ReviewId(3), new ProductId(2), "Written", today));
        var handler = new GetAverageScoresByProductIdsQueryHandler(repository);

        var averages = await handler.Handle(
            new GetAverageScoresByProductIdsQuery([new ProductId(1), new ProductId(2), new ProductId(3)]), default);

        Assert.Equal(3, averages[1]);
        Assert.False(averages.ContainsKey(2));
        Assert.False(averages.ContainsKey(3));
    }
}
