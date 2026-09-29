using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence.Repositories;

namespace WebShop.Reviews.Infrastructure.Tests;

public class ReviewPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<ReviewsContext> _options;

    public ReviewPersistenceTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<ReviewsContext>().UseSqlite(_connection).Options;
        using var setup = new ReviewsContext(_options);
        setup.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Anonymous_review_round_trips_with_a_null_reviewer()
    {
        using (var context = new ReviewsContext(_options))
        {
            var review = Review.Create(new ReviewId(1), new ProductId(1), "Video", DateOnly.FromDateTime(DateTime.Today));
            new ReviewRepository(context).Add(review);
            await context.SaveChangesAsync();
        }

        using var readContext = new ReviewsContext(_options);
        var loaded = await new ReviewRepository(readContext).GetById(new ReviewId(1), default);

        Assert.NotNull(loaded);
        Assert.Null(loaded.ReviewUserId);
    }

    [Fact]
    public async Task Attributed_review_round_trips_its_reviewer_and_score()
    {
        using (var context = new ReviewsContext(_options))
        {
            new ReviewUserRepository(context).Add(ReviewUser.Create(new ReviewUserId(1), "Alice"));
            var review = Review.Create(
                new ReviewId(1), new ProductId(1), "Written", DateOnly.FromDateTime(DateTime.Today),
                title: "Great product", score: 4.5m, reviewUserId: new ReviewUserId(1));
            new ReviewRepository(context).Add(review);
            await context.SaveChangesAsync();
        }

        using var readContext = new ReviewsContext(_options);
        var loaded = await new ReviewRepository(readContext).GetById(new ReviewId(1), default);

        Assert.NotNull(loaded);
        Assert.Equal(new ReviewUserId(1), loaded.ReviewUserId);
        Assert.Equal(4.5m, loaded.Score);
    }
}
