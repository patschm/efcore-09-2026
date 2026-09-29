using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebShop.Reviews.Domain.Aggregates;
using WebShop.Reviews.Domain.Identifiers;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence.Contexts;
using WebShop.Reviews.Infrastructure.SqlServer.Persistence.Repositories;

namespace WebShop.Reviews.Infrastructure.Tests;

public class ReviewUserPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<ReviewsContext> _options;

    public ReviewUserPersistenceTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<ReviewsContext>().UseSqlite(_connection).Options;
        using var setup = new ReviewsContext(_options);
        setup.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task ReviewUser_round_trips_with_a_null_email()
    {
        using (var context = new ReviewsContext(_options))
        {
            new ReviewUserRepository(context).Add(ReviewUser.Create(new ReviewUserId(1), "Alice"));
            await context.SaveChangesAsync();
        }

        using var readContext = new ReviewsContext(_options);
        var loaded = await new ReviewUserRepository(readContext).GetById(new ReviewUserId(1), default);

        Assert.NotNull(loaded);
        Assert.Equal("Alice", loaded.Name);
        Assert.Null(loaded.Email);
    }
}
